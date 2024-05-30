using GagSpeak.API.Data;
using GagSpeak.API.Data.Enum;
using GagSpeak.API.Dto;
using GagSpeak.API.SignalR;
using GagSpeakServer.Services;
using GagSpeakServer.Utils;
using GagSpeakShared;
using GagSpeakShared.Data;
using GagSpeakShared.Metrics;
using GagSpeakShared.Models;
using GagSpeakShared.Services;
using GagSpeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Concurrent;

namespace GagSpeakServer.Hubs;

// This class requires the user to be authenticated
[Authorize(Policy = "Authenticated")]
public partial class GagSpeakHub : Hub<IGagSpeakHub>, IGagSpeakHub
{
    // A thread-safe dictionary to store user connections
    private static readonly ConcurrentDictionary<string, string> _userConnections = new(StringComparer.Ordinal);
    
    // Metrics for monitoring the GagSpeak system
    private readonly GagSpeakMetrics _GagSpeakMetrics;
    
    // Service for getting system information
    private readonly SystemInfoService _systemInfoService;
    
    // Accessor to get HTTP context information
    private readonly IHttpContextAccessor _contextAccessor;
    
    // Logger specific to GagSpeakHub
    private readonly GagSpeakHubLogger _logger;
    
    // Name of the shard
    private readonly string _shardName;
    
    // Maximum number of groups a user can create
    private readonly int _maxExistingGroupsByUser;
    
    // Maximum number of groups a user can join
    private readonly int _maxJoinedGroupsByUser;
    
    // Maximum number of users a group can have
    private readonly int _maxGroupUserCount;
    
    // Redis database for caching
    private readonly IRedisDatabase _redis;
    
    // Service for managing online synced pairs
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;
    
    // Census for GagSpeak system
    private readonly GagSpeakCensus _GagSpeakCensus;
    
    // Address of the file server
    private readonly Uri _fileServerAddress;
    
    // Expected version of the client
    private readonly Version _expectedClientVersion;
    
    // Lazy initialization of GagSpeakDbContext
    private readonly Lazy<GagSpeakDbContext> _dbContextLazy;
    
    // Property to get the GagSpeakDbContext
    private GagSpeakDbContext DbContext => _dbContextLazy.Value;

    // Constructor for GagSpeakHub
    public GagSpeakHub(GagSpeakMetrics GagSpeakMetrics,
        IDbContextFactory<GagSpeakDbContext> GagSpeakDbContextFactory, ILogger<GagSpeakHub> logger, SystemInfoService systemInfoService,
        IConfigurationService<ServerConfiguration> configuration, IHttpContextAccessor contextAccessor,
        IRedisDatabase redisDb, OnlineSyncedPairCacheService onlineSyncedPairCacheService, GagSpeakCensus GagSpeakCensus)
    {
        // Assigning values to the fields
        _GagSpeakMetrics = GagSpeakMetrics;
        _systemInfoService = systemInfoService;
        _shardName = configuration.GetValue<string>(nameof(ServerConfiguration.ShardName));
        _maxExistingGroupsByUser = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxExistingGroupsByUser), 3);
        _maxJoinedGroupsByUser = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxJoinedGroupsByUser), 6);
        _maxGroupUserCount = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxGroupUserCount), 100);
        _fileServerAddress = configuration.GetValue<Uri>(nameof(ServerConfiguration.CdnFullUrl));
        _expectedClientVersion = configuration.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
        _contextAccessor = contextAccessor;
        _redis = redisDb;
        _onlineSyncedPairCacheService = onlineSyncedPairCacheService;
        _GagSpeakCensus = GagSpeakCensus;
        _logger = new GagSpeakHubLogger(this, logger);
        _dbContextLazy = new Lazy<GagSpeakDbContext>(() => GagSpeakDbContextFactory.CreateDbContext());
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_dbContextLazy.IsValueCreated) DbContext.Dispose();
        }

        base.Dispose(disposing);
    }

    [Authorize(Policy = "Identified")]
    public async Task<ConnectionDto> GetConnectionDto()
    {
        _logger.LogCallInfo();

        _GagSpeakMetrics.IncCounter(MetricsAPI.CounterInitializedConnections);

        await Clients.Caller.Client_UpdateSystemInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);

        var dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);
        dbUser.LastLoggedIn = DateTime.UtcNow;

        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information, "Welcome to GagSpeak Synchronos \"" + _shardName + "\", Current Online Users: " + _systemInfoService.SystemInfoDto.OnlineUsers).ConfigureAwait(false);

        var defaultPermissions = await DbContext.UserDefaultPreferredPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (defaultPermissions == null)
        {
            defaultPermissions = new UserDefaultPreferredPermission()
            {
                UserUID = UserUID,
            };

            DbContext.UserDefaultPreferredPermissions.Add(defaultPermissions);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        return new ConnectionDto(new UserData(dbUser.UID, string.IsNullOrWhiteSpace(dbUser.Alias) ? null : dbUser.Alias))
        {
            CurrentClientVersion = _expectedClientVersion,
            ServerVersion = IGagSpeakHub.ApiVersion,
            IsAdmin = dbUser.IsAdmin,
            IsModerator = dbUser.IsModerator,
            ServerInfo = new ServerInfo()
            {
                MaxGroupsCreatedByUser = _maxExistingGroupsByUser,
                ShardName = _shardName,
                MaxGroupsJoinedByUser = _maxJoinedGroupsByUser,
                MaxGroupUserCount = _maxGroupUserCount,
                FileServerAddress = _fileServerAddress,
            },
            DefaultPreferredPermissions = new DefaultPermissionsDto()
            {
                DisableGroupAnimations = defaultPermissions.DisableGroupAnimations,
                DisableGroupSounds = defaultPermissions.DisableGroupSounds,
                DisableGroupVFX = defaultPermissions.DisableGroupVFX,
                DisableIndividualAnimations = defaultPermissions.DisableIndividualAnimations,
                DisableIndividualSounds = defaultPermissions.DisableIndividualSounds,
                DisableIndividualVFX = defaultPermissions.DisableIndividualVFX,
                IndividualIsSticky = defaultPermissions.IndividualIsSticky,
            },
        };
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<bool> CheckClientHealth()
    {
        await UpdateUserOnRedis().ConfigureAwait(false);

        return false;
    }

    [Authorize(Policy = "Authenticated")]
    public override async Task OnConnectedAsync()
    {
        if (_userConnections.TryGetValue(UserUID, out var oldId))
        {
            _logger.LogCallWarning(GagSpeakHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            _userConnections[UserUID] = Context.ConnectionId;
        }
        else
        {
            _GagSpeakMetrics.IncGaugeWithLabels(MetricsAPI.GaugeConnections, labels: Continent);

            try
            {
                _logger.LogCallInfo(GagSpeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
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

    [Authorize(Policy = "Authenticated")]
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (_userConnections.TryGetValue(UserUID, out var connectionId)
            && string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            _GagSpeakMetrics.DecGaugeWithLabels(MetricsAPI.GaugeConnections, labels: Continent);

            try
            {
                await _onlineSyncedPairCacheService.DisposePlayer(UserUID).ConfigureAwait(false);

                _logger.LogCallInfo(GagSpeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                if (exception != null)
                    _logger.LogCallWarning(GagSpeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace));

                await RemoveUserFromRedis().ConfigureAwait(false);

                _GagSpeakCensus.ClearStatistics(UserUID);

                await SendOfflineToAllPairedUsers().ConfigureAwait(false);

                DbContext.RemoveRange(DbContext.Files.Where(f => !f.Uploaded && f.UploaderUID == UserUID));
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            catch { }
            finally
            {
                _userConnections.Remove(UserUID, out _);
            }
        }
        else
        {
            _logger.LogCallWarning(GagSpeakHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
