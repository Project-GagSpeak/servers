using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Enums;
using GagspeakAPI.SignalR;
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

public partial class GagspeakHub : Hub<IGagspeakHub>, IGagspeakHub
{
    // A thread-safe dictionary to store user connections (Shared across ALL instances of created gagspeak hub connections)
    public static readonly ConcurrentDictionary<string, string> _userConnections = new(StringComparer.Ordinal);

    // The Metrics for the GagSpeak web server
    private readonly GagspeakMetrics _metrics;

    // Service for getting system information
    private readonly SystemInfoService _systemInfoService;

    // Accessor to get HTTP context information
    private readonly IHttpContextAccessor _contextAccessor;

    // Logger specific to GagSpeakHub
    private readonly GagspeakHubLogger _logger;

    // Redis database for caching (Redi's allows us to have a simple way to store and manage user state with large scale)
    private readonly IRedisDatabase _redis;

    // Service for managing online synced pairs
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;

    // Expected version of the client
    private readonly Version _expectedClientVersion;

    // Lazy initialization of GagSpeakDbContext
    private readonly Lazy<GagspeakDbContext> _dbContextLazy;

    // Property to get the GagSpeakDbContext
    private GagspeakDbContext DbContext => _dbContextLazy.Value;

    // Constructor for GagspeakHub
    public GagspeakHub(GagspeakMetrics metrics,
        IDbContextFactory<GagspeakDbContext> GagSpeakDbContextFactory,
        ILogger<GagspeakHub> logger, SystemInfoService systemInfoService, IRedisDatabase redis,
        IConfigurationService<ServerConfiguration> configuration, IHttpContextAccessor contextAccessor,
        OnlineSyncedPairCacheService onlineSyncedPairCacheService)
    {
        _metrics = metrics;
        _systemInfoService = systemInfoService;
        _expectedClientVersion = configuration.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
        _contextAccessor = contextAccessor;
        _redis = redis;
        _onlineSyncedPairCacheService = onlineSyncedPairCacheService;
        _logger = new GagspeakHubLogger(this, logger);
        _dbContextLazy = new Lazy<GagspeakDbContext>(() => GagSpeakDbContextFactory.CreateDbContext());
    }

    /// <summary> Disposes of the database context if created upon the GagSpeak hub's disposal.</summary>
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
    /// Called by a connected client when they want to request a ConnectionDto object from the server.
    /// <para>
    /// This method required the requesting client to have the authorize policy "Identified" 
    /// (Meaning they have passed authorization) is bound to their request for the function to proceed. 
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<ConnectionDto> GetConnectionDto()
    {
        // log the caller who requested this method
        _logger.LogCallInfo();

        try
        {
            // a failsafe to make sure that any logged in account that is no longer in the DB cannot reconnect.
            bool userExists = DbContext.Users.Any(u => u.UID == UserUID || u.Alias == UserUID);
            if (!userExists)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error,
                    $"This secret key no longer exists in the DB. Inactive for too long.").ConfigureAwait(false);
                return null!;
            }

            // Send a client callback to the client caller with the systeminfo Dto.
            await Clients.Caller.Client_UpdateSystemInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);

            // Grab the user from the database whose UID reflects the UID of the client callers claims, and update last login time.
            User dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);
            dbUser.LastLoggedIn = DateTime.UtcNow;

            bool isVerified = await DbContext.AccountClaimAuth.AnyAsync(f => f.User.UID == UserUID).ConfigureAwait(false);

            // collect the list of auths for this user.
            List<string> accountProfileUids = await DbContext.Auth
                .Include(u => u.User)
                .Where(u => (u.UserUID != null && u.UserUID == UserUID) || (u.PrimaryUserUID != null && u.PrimaryUserUID == UserUID))
                .Select(u => u.UserUID!)
                .ToListAsync().ConfigureAwait(false);


            // Send a callback to the client caller with a welcome message, letting them know connection was sucessful.
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information,
                "Welcome to the CK Gagspeak Server! " + _systemInfoService.SystemInfoDto.OnlineUsers +
                " Kinksters are online.\nWe hope you enjoy your fun~").ConfigureAwait(false);

            // Ensure GlobalPerms.
            UserGlobalPermissions clientCallerGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(f => f.UserUID == UserUID).ConfigureAwait(false);
            if (clientCallerGlobalPerms is null)
            {
                clientCallerGlobalPerms = new UserGlobalPermissions() { UserUID = UserUID };
                DbContext.UserGlobalPermissions.Add(clientCallerGlobalPerms);
            }

            // Handle retrieving all GagData entries for the user, and correcting any invalid ones.
            List<UserGagData> gagStateCache = await DbContext.UserGagData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            for (byte layer = 0; layer < gagStateCache.Count; layer++)
            {
                if (gagStateCache[layer] is null)
                {
                    gagStateCache[layer] = new UserGagData() { UserUID = UserUID, Layer = layer };
                    DbContext.UserGagData.Add(gagStateCache[layer]);
                }
            }
            // Compile the API output.
            var gagDataApi = gagStateCache.Select(g => g.ToApiGagSlot()).ToArray();
            CharaActiveGags clientGags = new CharaActiveGags(gagDataApi);

            // Handle retrieving all RestrictionData entries for the user, and correcting any invalid ones.
            List<UserRestrictionData> restrictionStateCache = await DbContext.UserRestrictionData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            for (byte layer = 0; layer < restrictionStateCache.Count; layer++)
            {
                if (restrictionStateCache[layer] is null)
                {
                    restrictionStateCache[layer] = new UserRestrictionData() { UserUID = UserUID, Layer = layer };
                    DbContext.UserRestrictionData.Add(restrictionStateCache[layer]);
                }
            }
            // Compile the API output.
            var restrictionDataApi = restrictionStateCache.Select(r => r.ToApiRestrictionSlot()).ToArray();
            CharaActiveRestrictions clientRestrictions = new CharaActiveRestrictions(restrictionDataApi);

            // Handle retrieving the RestraintSetData entry for the user, and correcting it if invalid.
            UserRestraintData restraintSetData = await DbContext.UserRestraintData.SingleOrDefaultAsync(f => f.UserUID == UserUID).ConfigureAwait(false);
            if (restraintSetData is null)
            {
                restraintSetData = new UserRestraintData() { UserUID = UserUID };
                DbContext.UserRestraintData.Add(restraintSetData);
            }

            // grab the achievement data.
            UserAchievementData clientCallerAchievementData = await DbContext.UserAchievementData.SingleOrDefaultAsync(f => f.UserUID == UserUID).ConfigureAwait(false);
            if (clientCallerAchievementData is null)
            {
                clientCallerAchievementData = new UserAchievementData() { UserUID = UserUID, Base64AchievementData = null };
                DbContext.UserAchievementData.Add(clientCallerAchievementData);
            }

            // we will need to grab all of our published patterns and append them as PublishedPattern object to the connectionDto
            List<PatternEntry> callerPatternPublications = await DbContext.Patterns.Where(f => f.PublisherUID == UserUID).ToListAsync().ConfigureAwait(false);
            List<PublishedPattern> publishedPatterns = callerPatternPublications.Select(p => p.ToPublishedPattern()).ToList();

            // grab all the published moodles and append them as the published pattern object to the connectionDto
            List<MoodleStatus> callerMoodlePublications = await DbContext.Moodles.Where(f => f.PublisherUID == UserUID).ToListAsync().ConfigureAwait(false);
            List<PublishedMoodle> publishedMoodles = callerMoodlePublications.Select(m => m.ToPublishedMoodle()).ToList();

            // Save the DbContext (never know if it was added or not so always good to be safe.
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // now we can create the connectionDto object and return it to the client caller.
            return new ConnectionDto(dbUser.ToUserData(), isVerified)
            {
                CurrentClientVersion = _expectedClientVersion,
                ServerVersion = IGagspeakHub.ApiVersion,
                GlobalPerms = clientCallerGlobalPerms.ToApiGlobalPerms(),
                SyncedGagData = clientGags,
                SyncedRestrictionsData = clientRestrictions,
                SyncedRestraintSetData = restraintSetData.ToApiRestraintData(),
                PublishedPatterns = publishedPatterns,
                PublishedMoodles = publishedMoodles,
                ActiveAccountUidList = accountProfileUids,
                UserAchievements = clientCallerAchievementData.Base64AchievementData,
            };
        }
        catch (Exception ex)
        {
            // if we catch an error, log it and return null.
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "GetConnectionDto", ex.Message, ex.StackTrace ?? string.Empty));
            return null!;
        }
    }

    /// <summary> 
    /// Creates a new secret key and user for a client, which is called upon by their one time use request. 
    /// </summary>
    /// <returns> A tuple containing the UID and the hashed secret key for the one-time generation. </returns>
    [Authorize(Policy = "TemporaryAccess")]
    public async Task<(string, string)> OneTimeUseAccountGeneration()
    {
        // we will use this function to generate a new UID, and create an auth object for the user.
        // create a new user
        User user = new User() { CreatedDate = DateTime.UtcNow };

        // set has valid UID to false, so we can generate a new UID
        bool hasValidUid = false;
        while (!hasValidUid) // while its false, keep generating a new one.
        {
            string uid = StringUtils.GenerateRandomString(10);
            if (DbContext.Users.Any(u => u.UID == uid || u.Alias == uid)) continue;
            user.UID = uid;
            hasValidUid = true;
        }

        // set the last logged in time to now.
        user.LastLoggedIn = DateTime.UtcNow;

        // generate a hashed secret key based on a randomly generated string + the current time to make sure it is always unique.
#pragma warning disable MA0011 // IFormatProvider is missing
        string computedHash = StringUtils.Sha256String(StringUtils.GenerateRandomString(64) + DateTime.UtcNow.ToString());
#pragma warning restore MA0011 // IFormatProvider is missing

        // now we create a new authentication object with that hashed secret key in it and the user object.
        Auth auth = new Auth()
        {
            HashedKey = computedHash,
            User = user,
        };

        // append them to the database
        await DbContext.Users.AddAsync(user).ConfigureAwait(false);
        await DbContext.Auth.AddAsync(auth).ConfigureAwait(false);

        // log that we registered a new user
        _logger.LogMessage($"User registered with UID: {user.UID}  || and secret key: {computedHash}");

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // return the user UID and the hashed secret key to the client who requested it.
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
    public async Task<bool> CheckMainClientHealth()
    {
        // _logger.LogMessage("CheckingClientHealth -- Also Updating User on Redis"); <--- Terrible idea to log this because it will log once for every user online lol.
        await UpdateUserOnRedis().ConfigureAwait(false);

        return false;
    }


    /// <summary> 
    /// Called by a client once they are fully connected to the server. Overrides original OnConnectedASync from base hub
    /// <para>
    /// The _userConnections is the concurrent dictionary of connected users to the server.
    /// </para>
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
            // if we got here log, a warning that we are updating the users UID to the new connection 
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            // Update concurrent dictionary with unique ID
            _userConnections[UserUID] = Context.ConnectionId;
            // add the user to the global chat group
            await Groups.AddToGroupAsync(Context.ConnectionId, GagspeakGlobalChat).ConfigureAwait(false);
        }
        // otherwise, this is a new connection, so lets establish it.
        else
        {
            // next up, try and log the connection attempt with the user details
            try
            {
                // display IP of client who just connected, and initialize player into the online synced pair cache service.
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                await _onlineSyncedPairCacheService.InitPlayer(UserUID).ConfigureAwait(false);
                // Finally, update the user onto the redi's server, then set their connection ID in the concurrent dictionary.
                await UpdateUserOnRedis().ConfigureAwait(false);
                _userConnections[UserUID] = Context.ConnectionId;
                // add the user to the global chat group
                await Groups.AddToGroupAsync(Context.ConnectionId, GagspeakGlobalChat).ConfigureAwait(false);
            }
            catch
            {
                // if at any point we catch an error, then remove the user from the concurrent dictionary of user connections.

                _userConnections.Remove(UserUID, out _);
                // remove user from the global chat group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GagspeakGlobalChat).ConfigureAwait(false);
            }
        }
        await base.OnConnectedAsync().ConfigureAwait(false);
    }


    /// <summary> 
    /// Called by a client when they disconnect from the server.
    /// <para>
    /// This method ensure everything is properly disconnected once the function is called upon.
    /// Note that we dont require the authenticated policy for disconnect because the temp access could be using it as well.
    /// </para>
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        /* -------------------- Temporary Connection -------------------- */
        // if its a temp connection disconnecting, simply call the base and exit
        if (string.Equals(UserHasTempAccess, "LocalContent", StringComparison.Ordinal))
        {
            _logger.LogMessage("Temp Access Connection Disconnected.");
            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
            return;
        }

        /* -------------------- Regular Connection -------------------- */
        // Attempt to retrieve an existing connection ID for the user UID.
        if (_userConnections.TryGetValue(UserUID, out string connectionId)
            && string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            // if they were already in the dictionary, log that we have a user disconnecting from the current connection total
            _logger.LogMessage("Removing Connection of 1 user.");

            try
            {
                // dispose the player from the online synced pair cache service
                await _onlineSyncedPairCacheService.DisposePlayer(UserUID).ConfigureAwait(false);

                // log the call info of the user who disconnected
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));

                // check to see if they disconnected with an exception. If it did, log it as a warning message
                if (exception != null)
                {
                    _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace ?? string.Empty));
                }

                // remove the users from the redis database (if it is critical to the discord bot)
                await RemoveUserFromRedis().ConfigureAwait(false);

                // send a function call to all connected pairs of this user that they have gone offline.
                await SendOfflineToAllPairedUsers().ConfigureAwait(false);
            }
            catch { /* Consume */ }
            finally
            {
                // finally, remove this user from the concurrent dictionary of connected users.
                _userConnections.Remove(UserUID, out _);
                // remove user from the global chat group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GagspeakGlobalChat).ConfigureAwait(false);
            }
        }
        // if we reach here, we should log a warning that the user disconnecting was not in the dictionary of connected users.
        else
        {
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        // await the base disconnectedAsync method to occur.
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

