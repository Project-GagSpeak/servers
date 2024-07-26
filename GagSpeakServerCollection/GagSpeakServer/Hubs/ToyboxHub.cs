using GagspeakAPI.Data.Enum;
using GagspeakAPI.SignalR;
using GagspeakAPI.Dto.Connection;
using GagspeakServer.Services;
using GagspeakServer.Utils;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Concurrent;

namespace GagspeakServer.Hubs;


public partial class ToyboxHub : Hub<IToyboxHub>, IToyboxHub
{
    /// <summary>
    /// Thread-safe dictionary to store user connections.
    /// Key = UserUID, Value = ConnectionId
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> _toyboxUserConnections = new(StringComparer.Ordinal);
    
    // Key = RoomName, Value = HashSet of UserUIDs
    private static readonly ConcurrentDictionary<string, HashSet<string>> RoomContextGroupVibeUsers = new(StringComparer.Ordinal);

    // Key = RoomName, Value = Host UserUID
    private static readonly ConcurrentDictionary<string, string> RoomHosts = new(StringComparer.Ordinal);



    // The Metrics for the GagSpeak web server
    private readonly GagspeakMetrics _metrics;

    // Service for getting system information
    private readonly SystemInfoService _systemInfoService;

    // Accessor to get HTTP context information
    private readonly IHttpContextAccessor _contextAccessor;

    // Logger specific to ToyboxHub
    private readonly ToyboxHubLogger _logger;

    // Redis database for updating connected users and their connection ID's to the redi's server.
    // This allows for easy management when users are connected to multiple paths.
    private readonly IRedisDatabase _redis;

    // Service for managing online synced pairs. This ensures existing communications made between a client caller,
    // and their paired users are maintained. This is VERY VERY BENIFICIAL FOR REALTIME UPDATES.
    // However, it's worth noting that this already contains our connected clients, as a requirement to be connected here is
    // to be established on the GagSpeak hub.
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;

    // Lazy initialization of GagSpeakDbContext
    private readonly Lazy<GagspeakDbContext> _dbContextLazy;

    // Property to get the GagSpeakDbContext
    private GagspeakDbContext DbContext => _dbContextLazy.Value;

    // Constructor for ToyboxHub
    public ToyboxHub(GagspeakMetrics metrics,IDbContextFactory<GagspeakDbContext> GagSpeakDbContextFactory,
        ILogger<ToyboxHub> logger, SystemInfoService systemInfoService, IRedisDatabase redis,
        IConfigurationService<ServerConfiguration> configuration, IHttpContextAccessor contextAccessor,
        OnlineSyncedPairCacheService onlineSyncedPairCacheService)
    {
        _metrics = metrics;
        _systemInfoService = systemInfoService;
        _contextAccessor = contextAccessor;
        _redis = redis;
        _onlineSyncedPairCacheService = onlineSyncedPairCacheService;
        _logger = new ToyboxHubLogger(this, logger);
        _dbContextLazy = new Lazy<GagspeakDbContext>(() => GagSpeakDbContextFactory.CreateDbContext());
    }

    /// <summary> Disposes of the database context if created upon the Toybox hub's disposal.</summary>
    protected override void Dispose(bool disposing)
    {
        // if disposing is true
        if (disposing)
        {
            // and the lazy value is created, dispose of the database context
            if (_dbContextLazy.IsValueCreated) DbContext.Dispose();
        }
        // then dispose the base with true set
        base.Dispose(disposing);
    }

    /// <summary> 
    /// Called by a client when they wish to fetch a connectionDto from the toybox server.
    /// Will not send back a system info dto
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<ToyboxConnectionDto> GetToyboxConnectionDto()
    {
        // log the caller who requested this method
        _logger.LogCallInfo();

        // increase the counter for initialized connections
        _metrics.IncCounter(MetricsAPI.CounterInitializedConnections);

        // a failsafe to make sure that any logged in account that is no longer in the DB cannot reconnect.
        var userExists = DbContext.Users.Any(u => u.UID == UserUID || u.Alias == UserUID);
        if (!userExists)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Error,
                $"This secret key no longer exists in the DB. Inactive for too long.").ConfigureAwait(false);
            return null;
        }

        // Grab the user from the database whose UID reflects the UID of the client callers claims, and update last login time.
        User dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);

        // Send a callback to the client caller with a welcome message, letting them know connection was sucessful.
        await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Information,
            "Connected to CK's Toybox Server! " + _systemInfoService.SystemInfoDto.OnlineUsers +
            " Horny users are connected. Enjoy yourselves~").ConfigureAwait(false);

        // now we can create the connectionDto object and return it to the client caller.
        return new ToyboxConnectionDto(dbUser.ToUserData()) { ServerVersion = IGagspeakHub.ApiVersion };
    }

    /// <summary> 
    /// Updates the client caller user on the redi's server
    /// </summary>
    [Authorize(Policy = "Authenticated")]
    public async Task<bool> CheckToyboxClientHealth()
    {
        await UpdateUserOnRedis().ConfigureAwait(false);

        return false;
    }


    /// <summary> Called when client caller connects to the toybox hub. </summary>
    public override async Task OnConnectedAsync()
    {
        // Attempt to retrieve an existing connection ID for the user UID.
        // If it exists, it means they are already connected.
        if (_toyboxUserConnections.TryGetValue(UserUID, out var oldId))
        {
            // if we got here log, a warning that we are updating the users UID to the new connection 
            _logger.LogCallWarning(ToyboxHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            // Update concurrent dictionary with unique ID
            _toyboxUserConnections[UserUID] = Context.ConnectionId;
        }
        // otherwise, this is a new connection, so lets establish it.
        else
        {
            try
            {
                // display IP of client who just connected, and initialize player into the online synced pair cache service.
                _logger.LogCallInfo(ToyboxHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                // Update User on Redi's, sending them online for client health checks and for discord monitoring of toybox hub.
                await UpdateUserOnRedis().ConfigureAwait(false);
                // Add the user to the concurrent dictionary of user connections.
                _toyboxUserConnections[UserUID] = Context.ConnectionId;
                // try and loopup if the user is a part of any PrivateRooms
                var userRoom = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync(pru => pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);
                // see if they are currently connected to that groups hubcontext group.
                if (userRoom != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, userRoom.PrivateRoomNameID).ConfigureAwait(false);
                }
            }
            catch
            {
                // if at any point we catch an error, then remove the user from the concurrent dictionary of user connections.
                _toyboxUserConnections.Remove(UserUID, out _);
            }
        }
        await base.OnConnectedAsync().ConfigureAwait(false);
    }


    /// <summary> Called when client caller disconnects from the toybox hub. </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        /* -------------------- Regular Connection -------------------- */
        // Attempt to retrieve an existing connection ID for the user UID.
        if (_toyboxUserConnections.TryGetValue(UserUID, out var connectionId)
            && string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            // if they were already in the dictionary, log that we have a user disconnecting from the current connection total
            _logger.LogMessage("Removing Connection of 1 user from ToyboxHub Only. (Still online in main server).");

            try
            {
                // log the call info of the user who disconnected
                _logger.LogCallInfo(ToyboxHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));

                // check to see if they disconnected with an exception. If it did, log it as a warning message
                if (exception != null)
                {
                    _logger.LogCallWarning(ToyboxHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace));
                }

                // Remove User from Redi's, sending them offline for client health checks and for discord monitoring.
                await RemoveUserFromRedis().ConfigureAwait(false);
            }
            catch { /* Consume */ }
            finally
            {
                // remove the user from the toybox concurrent dictionary.
                _toyboxUserConnections.Remove(UserUID, out _);
                // remove them from the group room they were in
                // try and loopup if the user is a part of any PrivateRooms
                var userRoom = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync(pru => pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);
                // see if they are currently connected to that groups hubcontext group.
                if (userRoom != null)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRoom.PrivateRoomNameID).ConfigureAwait(false);
                }

            }
        }
        // if we reach here, we should log a warning that the user disconnecting was not in the dictionary of connected users.
        else
        {
            _logger.LogCallWarning(ToyboxHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        // await the base disconnectedAsync method to occur.
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

