using GagspeakAPI.Connection;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
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
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Concurrent;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0011

public partial class GagspeakHub : Hub<IGagspeakHub>, IGagspeakHub
{
    // A thread-safe dictionary to store user connections (Shared across ALL instances of created gagspeak hub connections)
    public static readonly ConcurrentDictionary<string, string> _userConnections = new(StringComparer.Ordinal);

    // Services for GagSpeak
    private readonly GagspeakHubLogger _logger;
    private readonly GagspeakMetrics _metrics;
    private readonly ChatService _chatService;
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;
    private readonly SystemInfoService _systemInfoService;

    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IRedisDatabase _redis;

    // Lazily initialized DbContext generated on initialization.
    // Does not technically need to be lazy as it is not modified, but useful to have.
    private readonly Lazy<GagspeakDbContext> _dbContextLazy;

    private GagspeakDbContext DbContext => _dbContextLazy.Value;
    private readonly Version _expectedClientVersion;

    public GagspeakHub(
        ILogger<GagspeakHub> logger,
        IDbContextFactory<GagspeakDbContext> dbFactory,
        IConfigurationService<ServerConfiguration> config,
        GagspeakMetrics metrics,
        ChatService chatService,
        OnlineSyncedPairCacheService onlineSyncService,
        SystemInfoService systemInfoService,
        IRedisDatabase redis,
        IHttpContextAccessor contextAccessor)
    {
        _logger = new GagspeakHubLogger(this, logger);
        _metrics = metrics;
        _chatService = chatService;
        _onlineSyncedPairCacheService = onlineSyncService;
        _systemInfoService = systemInfoService;
        _contextAccessor = contextAccessor;
        _redis = redis;

        _dbContextLazy = new Lazy<GagspeakDbContext>(() => dbFactory.CreateDbContext());

        _expectedClientVersion = config.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _dbContextLazy.IsValueCreated)
            DbContext.Dispose();

        base.Dispose(disposing);
    }

    /// <summary> 
    ///   Called by a connected client when they want to request a GetConnectionResponse object from the server. <para />
    ///   
    ///   Requires the requesting client to have authorization policy of "Identified" to proceed, meaning they used the
    ///   one-time use account generation to create a new user and secret key.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<ConnectionResponse> GetConnectionResponse()
    {
        // log the caller who requested this method
        _logger.LogCallInfo();

        // Fail if Auth is not present.
        if (await DbContext.Auth.AsNoTracking().Include(a => a.AccountRep).Include(a => a.User).FirstOrDefaultAsync(a => a.UserUID == UserUID).ConfigureAwait(false) is not { } auth)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, $"Secret key no longer exists in the DB. Inactive for too long.").ConfigureAwait(false);
            return null!;
        }
           
        _metrics.IncCounter(MetricsAPI.CounterInitializedConnections);
        await Clients.Caller.Callback_ServerInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);
        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Welcome to Gagspeak! " +
            $"{_systemInfoService.SystemInfoDto.OnlineUsers} Kinksters are online.\nI hope you have fun and enjoy~").ConfigureAwait(false);

        // All connected clients need a list of their account profile UID's and their global permissions, hardcore state, active states, and rep.

        // Determine if the auth is the primary account profile or alt profile. Gather the account list based on this.
        var accountProfiles = await DbContext.Auth.AsNoTracking().Include(a => a.AccountRep)
            .Where(a => a.PrimaryUserUID == auth.PrimaryUserUID)
            .ToListAsync()
            .ConfigureAwait(false);

        // Handle cases where the SingleAsync fails. But this should never fail as all are made upon profile creation.
        try
        {
            var globals = await DbContext.GlobalPermissions.AsNoTracking().SingleAsync(g => g.UserUID == UserUID).ConfigureAwait(false);
            var hcState = await DbContext.HardcoreState.AsNoTracking().SingleAsync(h => h.UserUID == UserUID).ConfigureAwait(false);
            var gags = await DbContext.ActiveGagData.AsNoTracking().Where(g => g.UserUID == UserUID).OrderBy(g => g.Layer).ToListAsync().ConfigureAwait(false);
            var restrictions = await DbContext.ActiveRestrictionData.AsNoTracking().Where(r => r.UserUID == UserUID).OrderBy(r => r.Layer).ToListAsync().ConfigureAwait(false);
            var restraint = await DbContext.ActiveRestraintData.AsNoTracking().SingleAsync(r => r.UserUID == UserUID).ConfigureAwait(false);
            var collar = await DbContext.ActiveCollarData.AsNoTracking().Include(c => c.Owners).SingleAsync(c => c.UserUID == UserUID).ConfigureAwait(false);
            var achievements = await DbContext.AchievementData.AsNoTracking().SingleAsync(a => a.UserUID == UserUID).ConfigureAwait(false);

            // Ret the connection response data.
            return new ConnectionResponse(auth.User.ToUserData(), accountProfiles.Select(ap => ap.UserUID).ToList())
            {
                CurrentClientVersion = _expectedClientVersion,
                ServerVersion = IGagspeakHub.ApiVersion,

                Reputation = auth.AccountRep.ToApi(),

                GlobalPerms = globals.ToApi(),
                HardcoreState = hcState.ToApi(),
                GagData = new CharaActiveGags(gags.Select(g => g.ToApiGagSlot()).ToArray()),
                RestrictionsData = new CharaActiveRestrictions(restrictions.Select(r => r.ToApiRestrictionSlot()).ToArray()),
                RestraintSetData = restraint.ToApiRestraintData(),
                CollarData = collar.ToApiCollarData(),

                UserAchievements = achievements.Base64AchievementData,
            };
        }
        catch (Exception ex)
        {
            // if we catch an error, log it and return null.
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "GetConnectionResponse", ex.Message, ex.StackTrace ?? string.Empty));
            return null!;
        }
    }


    /// <summary>
    ///   This only returns the data that is respective to the caller's UserUID
    /// </summary>
    /// <returns> All the UserUID's published patterns, loci statuses, and collective ShareHub tags for searches. </returns>
    [Authorize(Policy = "Identified")]
    public async Task<LobbyAndHubInfoResponse> GetShareHubAndLobbyInfo()
    {
        // Request these in
        var callerPatternUploads = (await DbContext.Patterns.AsNoTracking().Where(f => f.PublisherUID == UserUID).ToListAsync().ConfigureAwait(false)).Select(p => p.ToPublishedPattern()).ToList();
        var callerLociUploads = (await DbContext.LociStatuses.AsNoTracking().Where(f => f.PublisherUID == UserUID).ToListAsync().ConfigureAwait(false)).Select(m => m.ToPublishedMoodle()).ToList();
        var tags = await DbContext.Keywords.AsNoTracking().Select(k => k.Word).ToListAsync().ConfigureAwait(false);

        HashEntry[] entries = await _redis.Database.HashGetAllAsync(VibeRoomRedis.RoomInviteKey(UserUID)).ConfigureAwait(false);
        var invites = entries.Select(e => new RoomInvite(new(UserUID), e.Name.ToString(), e.Value.ToString())).ToList();

        return new LobbyAndHubInfoResponse(tags)
        {
            PublishedPatterns = callerPatternUploads,
            PublishedLociData = callerLociUploads,
            RoomInvites = invites,
        };
    }

    /// <summary> 
    ///   Creates a new secret key and user for a client, which is called upon by their one time use request. 
    /// </summary>
    /// <returns> A tuple containing the UID and the hashed secret key for the one-time generation. </returns>
    [Authorize(Policy = "TemporaryAccess")]
    public async Task<(string, string)> OneTimeUseAccountGeneration()
    {
        // Need to create the User & Auth entries first before generating all associated content.
        // Ensure UID Uniqueness.
        var user = new User()
        {
            LastLogin = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // set has valid UID to false, so we can generate a new UID
        bool hasValidUid = false;
        while (!hasValidUid) // while its false, keep generating a new one.
        {
            string uid = StringUtils.GenerateRandomString(10);
            if (DbContext.Users.Any(u => u.UID == uid || u.Alias == uid)) continue;
            user.UID = uid;
            hasValidUid = true;
        }

        // Make an AccountRep entry for this user.
        var reputation = new AccountReputation()
        {
            UserUID = user.UID,
            User = user,
        };

        // Gen secret key 64long plus the current time.
        string computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
        // Add the auth for this user. In this case, the auth created is always the account's auth.
        // Add the respective Auth, referencing the User.
        var auth = new Auth()
        {
            HashedKey = computedHash, // This should technically be hashed again but it would disrupt how all other client's hashes currently are.
            UserUID = user.UID,
            User = user,
            PrimaryUserUID = user.UID,
            PrimaryUser = user,
            AccountRep = reputation,
        };

        // run the shared database function to create the new profile data.
        await SharedDbFunctions.CreateMainProfile(user, reputation, auth, _logger.Logger, DbContext, _metrics).ConfigureAwait(false);
        
        _logger.LogMessage($"Created User [{user.UID} (Alias: {user.Alias})] Key -> {computedHash}");
        return (user.UID, computedHash);
    }


    /// <summary> 
    /// Called by a connected client when they want to check if the client is healthy.
    /// <para> 
    /// This method required the requesting client to have the authorize policy "Authenticated"
    /// It should technically be updating the user on redi's but for now we wont worry about it 
    /// (unless it is critical to the discord bot)
    /// </para>
    /// </summary>
    [Authorize(Policy = "Authenticated")]
    public async Task<bool> HealthCheck()
    {
        await UpdateUserOnRedis().ConfigureAwait(false);
        return false;
    }


    /// <summary> 
    ///   Called after fully connecting to GagSpeak servers. <para />
    ///   The _userConnections is the concurrent dictionary of connected users to the server.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        /* -------------------- Temporary Connection -------------------- */
        // perform a check to see if the connection being made is from a user requesting a one-time temp access
        if (string.Equals(UserHasTempAccess, "LocalContent", StringComparison.Ordinal))
        {
            // allow the connection but dont store the user connection
            _logger.LogMessage("Temp Access Connection Established.");
            await base.OnConnectedAsync().ConfigureAwait(false);
            return;
        }
        /* -------------------- Regular Connection -------------------- */
        // Attempt to retrieve an existing connection ID for the user UID.
        // If it exists, it means they are already connected.
        if (_userConnections.TryGetValue(UserUID, out string oldId))
        {
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            _userConnections[UserUID] = Context.ConnectionId;
        }
        // otherwise, attempt establishing a new connection.
        else
        {
            _metrics.IncGauge(MetricsAPI.GaugeConnections);
            // If this fails we should remove the user from the group and user connections.
            try
            {
                // display IP of client who just connected, and initialize player into the online synced pair cache service.
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                // initialize the online synced pair cache service with the user UID and connection ID.
                await _onlineSyncedPairCacheService.InitPlayer(UserUID).ConfigureAwait(false);
                await UpdateUserOnRedis().ConfigureAwait(false);
                _userConnections[UserUID] = Context.ConnectionId;
            }
            catch
            {
                _userConnections.Remove(UserUID, out _);
            }
        }
        await base.OnConnectedAsync().ConfigureAwait(false);
    }


    /// <summary> 
    ///   Properly disposes of any connected or lingering data of a disconnected user.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        /* ---- Temporary Connection ---- */
        if (string.Equals(UserHasTempAccess, "LocalContent", StringComparison.Ordinal))
        {
            _logger.LogMessage("Temp Access Connection Disconnected.");
            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
            return;
        }

        /* -------------------- Regular Connection -------------------- */
        // Attempt to retrieve an existing connection ID for the user UID.
        if (_userConnections.TryGetValue(UserUID, out string cid) && string.Equals(cid, Context.ConnectionId, StringComparison.Ordinal))
        {
            _metrics.DecGauge(MetricsAPI.GaugeConnections);
            try
            {
                // try to leave the current room if the user is in one prior to removing them from redi's connection.
                await RoomLeave().ConfigureAwait(false);

                // dispose from cache sync service
                await _onlineSyncedPairCacheService.DisposePlayer(UserUID).ConfigureAwait(false);

                // log the call info of the user who disconnected
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                if (exception != null)
                    _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace ?? string.Empty));

                // remove the users from the redis database (if it is critical to the discord bot)
                await RemoveUserFromRedis().ConfigureAwait(false);

                // send a function call to all connected pairs of this user that they have gone offline.
                await SendOfflineToKinksterPairs().ConfigureAwait(false);
            }
            catch { /* Consume */ }
            finally
            {
                // finally, remove this user from the concurrent dictionary of connected users.
                _userConnections.Remove(UserUID, out _);
            }
        }
        // Log warning if a DC happened before we were even properly established in _userConnections.
        else
        {
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

