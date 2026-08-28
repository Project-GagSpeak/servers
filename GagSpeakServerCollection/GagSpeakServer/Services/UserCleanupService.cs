using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.User;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Globalization;
using System.Text.Json;

namespace GagspeakServer.Services;
/// <summary> Service for cleaning up users and groups that are no longer active </summary>
public class UserCleanupService : BackgroundService
{
    private readonly GagspeakMetrics _metrics;
    private readonly ChatService _chatService;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly ILogger<UserCleanupService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly IRedisDatabase _redis;

    public UserCleanupService(GagspeakMetrics metrics, ChatService chatService,
        IConfigurationService<ServerConfiguration> config, IDbContextFactory<GagspeakDbContext> dbContextFactory,
        ILogger<UserCleanupService> logger, IHubContext<GagspeakHub, IGagspeakHub> hubContext, IRedisDatabase redis)
    {
        _metrics = metrics;
        _chatService = chatService;
        _config = config;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _hubContext = hubContext;
        _redis = redis;
    }

    internal static DateTime UploadResetTimeEpoc => new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleanup Service started");
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }
 
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Do once only.
        using (GagspeakDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
        {
            // Update all statuses from v2 to v3, if they exist. This ports them to LociStatusInfo
            var outdatedStatuses = await db.LociStatuses.Where(m => m.Version < 3).ToListAsync(ct).ConfigureAwait(false);
            if (outdatedStatuses.Count > 0)
            {
                foreach (var legacy in outdatedStatuses)
                {
                    legacy.Version = 3;
                    legacy.StackToChain = 1;
                    legacy.ChainType = 0;
                }
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogWarning($"Migrated {outdatedStatuses.Count} legacy Moodle formats to LociStatus format.");
            }
        }

        // These should be checked frequently.
        var task1 = RunLoopAsync(TimeSpan.FromMinutes(15), "Auth Claims", CleanUpTimedOutAccountAuthClaims, ct);

        var task2 = RunLoopAsync(TimeSpan.FromMinutes(30), "Pair Requests", CleanupOutdatedPairRequests, ct);

        // We can process these timeouts much slower.
        var task3 = RunLoopAsync(TimeSpan.FromHours(1), "Old VibeRooms", CleanUpOldRooms, ct);


        // 4 times a day run a cleanup on the chats. (Any chats not interacted for the last 4 days)
        // NOTE: Could modify this to be async and check the owners of said chats if sanction chats, to give supporters longer expiration days.
        var task4 = RunLoopAsync(TimeSpan.FromHours(6), "Inactive Chats", _ => _chatService.PurgeInactiveChats(TimeSpan.FromDays(4)), ct);

        // 4 times a day run a check for resetting upload counters
        var task5 = RunLoopAsync(TimeSpan.FromHours(6), "Upload Counters", ResetUploadCounters, ct);

        // Once a day sweep unused accounts.
        var task6 = RunLoopAsync(TimeSpan.FromDays(1), "Unused Accounts", PurgeUnusedAccounts, ct);

        // Execution completes when the tasks complete, which would occur if the token is canceled.
        await Task.WhenAll(task1, task2, task3, task4, task5, task6).ConfigureAwait(false);
    }

    /// <summary>
    ///   Performs a cleanup task every specified interval, logging any exceptions that occur during execution.
    /// </summary>
    private async Task RunLoopAsync(TimeSpan interval, string taskName, Func<GagspeakDbContext, Task> action, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                await action(dbContext).ConfigureAwait(false);
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred while executing cleanup task: {taskName}", taskName);
            }
        }
    }

    /// <summary>
    ///   An overload for <see cref="RunLoopAsync(TimeSpan,string,Func{GagspeakDbContext,Task},CancellationToken)"/> that takes synchronous actions.
    /// </summary>
    private async Task RunLoopAsync(TimeSpan interval, string taskName, Action<GagspeakDbContext> action, CancellationToken ct)
    {
        await RunLoopAsync(interval, taskName, db =>
        {
            action(db);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);
    }

    #region Upload Counters
    private async Task ResetUploadCounters(GagspeakDbContext dbContext)
    {
        // Get the last reset datetimeUTC, compare against current, to get if we should reset counters.
        var utcNow = DateTime.UtcNow;
        // get how many full weeks passed since epoch.
        int weeksSinceEpoch = (int)((DateTime.UtcNow - UploadResetTimeEpoc).TotalDays / 7);
        // Last deterministic reset time.
        var lastResetUtc = UploadResetTimeEpoc.AddDays(weeksSinceEpoch * 7);
        // Get the time since the last reset period.
        var timeSinceLastReset = utcNow - lastResetUtc;

        // If less than 7 days since last reset, skip.
        if (timeSinceLastReset.TotalDays < 7)
            return;

        // Otherwise, we should reset all upload counters for the users.
        try
        {
            _logger.LogInformation("[ResetUploadCounters] Resetting upload counts for users.");
            // Grab all auths, including the rep and user.
            await dbContext.Auth
                .Include(a => a.AccountRep)
                .ThenInclude(a => a.User)
                .ExecuteUpdateAsync(ar => ar.SetProperty(
                    a => a.AccountRep.UploadAllowances,
                    // Check no role fist, as people will more likely match the first case.
                    a => a.User.Tier == CkVanityTier.NoRole ? 10 :
                         a.User.Tier == CkVanityTier.KinkporiumMistress ? 999999 :
                         a.User.Tier == CkVanityTier.DistinguishedConnoisseur ? 20 :
                         a.User.Tier == CkVanityTier.EsteemedPatron ? 15 :
                         a.User.Tier == CkVanityTier.ServerBooster ? 15 :
                         12
                )).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during upload counter reset");
        }
    }
    #endregion

    #region VibeRooms
    private async Task CleanUpOldRooms(GagspeakDbContext dbContext)
    {
        DateTime nowUtc = DateTime.UtcNow;

        try
        {
            IEnumerable<string> roomKeys = await _redis.SearchKeysAsync("VibeRoom:Room:*").ConfigureAwait(false);
            foreach (string roomHashKey in roomKeys)
            {
                string roomKeyStr = roomHashKey.ToString();
                string roomName = roomKeyStr.Substring("VibeRoom:Room:".Length);

                // get the creation time of the room.
                RedisValue createdTimeValue = await _redis.Database.HashGetAsync(roomHashKey, "CreatedTimeUTC").ConfigureAwait(false);
                if (createdTimeValue.IsNullOrEmpty)
                    continue;

                // Parse and check the age
                if (!DateTime.TryParse(createdTimeValue.ToString(), null, DateTimeStyles.AdjustToUniversal, out DateTime createdAt))
                    continue;

                if ((nowUtc - createdAt).TotalHours < 8)
                    continue;

                _logger.LogInformation($"Cleaning up old Redis VibeRoom: {roomName}");

                // --- Cleanup logic ---
                // Begin by removing all participants from the room.
                string participantsKey = VibeRoomRedis.ParticipantsKey(roomName);
                RedisValue[] participantUids = await _redis.Database.SetMembersAsync(participantsKey).ConfigureAwait(false);
                await _redis.Database.KeyDeleteAsync(participantsKey).ConfigureAwait(false);

                // Remove each participant’s data
                foreach (RedisValue uid in participantUids)
                {
                    string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, uid!);
                    await _redis.Database.KeyDeleteAsync(participantDataKey).ConfigureAwait(false);
                    await _redis.Database.KeyDeleteAsync(VibeRoomRedis.KinksterRoomKey(uid.ToString())).ConfigureAwait(false);
                }

                // Delete participants set
                await _redis.Database.KeyDeleteAsync(participantsKey).ConfigureAwait(false);

                // Remove host pointer.
                await _redis.Database.KeyDeleteAsync(VibeRoomRedis.RoomHostKey(roomName)).ConfigureAwait(false);

                // Remove the room metadata/hash.
                await _redis.Database.KeyDeleteAsync(roomHashKey).ConfigureAwait(false);

                // remove the public room index.
                await _redis.Database.SetRemoveAsync(VibeRoomRedis.PublicRoomsKey, roomHashKey).ConfigureAwait(false);

                // remove the tag indexes.
                RedisValue tagJson = await _redis.Database.HashGetAsync(roomHashKey, "Tags").ConfigureAwait(false);
                if (!tagJson.IsNullOrEmpty && JsonSerializer.Deserialize<List<string>>(tagJson.ToString()) is { } roomTags)
                {
                    // Remove the room from the tag indexes.
                    foreach (string tag in roomTags)
                    {
                        string tagKey = VibeRoomRedis.TagIndexKey(tag);
                        await _redis.Database.SetRemoveAsync(tagKey, roomHashKey).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Redis old rooms cleanup");
        }
    }
    #endregion

    #region Unused Accounts
    private async Task PurgeUnusedAccounts(GagspeakDbContext dbContext)
    {
        if (_config.GetValueOrDefault(nameof(ServerConfiguration.PurgeUnusedAccounts), false))
        {
            int usersOlderThanDays = _config.GetValueOrDefault(nameof(ServerConfiguration.PurgeUnusedAccountsPeriodInDays), 300);

            _logger.LogInformation("Cleaning up users older than {usersOlderThanDays} days", usersOlderThanDays);

            IQueryable<User> allUsers = dbContext.Users.AsNoTracking().Where(u => string.IsNullOrEmpty(u.Alias));
            List<User> usersToRemove = new List<User>();
            foreach (User user in allUsers)
            {
                if (user.LastLogin < DateTime.UtcNow - TimeSpan.FromDays(usersOlderThanDays))
                {
                    _logger.LogInformation("User outdated: {userUID}", user.UID);
                    usersToRemove.Add(user);
                }
            }

            foreach (User user in usersToRemove)
            {
                Dictionary<string, List<string>> remUserPairUidDict = await SharedDbFunctions.DeleteUserProfile(user, _logger, dbContext, _metrics).ConfigureAwait(false);
                // inform all related pairs to remove the user.
                foreach ((string removedUser, List<string> removedPairUids) in remUserPairUidDict)
                    await _hubContext.Clients.Users(removedPairUids).Callback_RemovePair(new(new(removedUser))).ConfigureAwait(false);
            }
        }
        _logger.LogInformation("Cleaning up unauthorized users");
    }
    #endregion

    #region Outdated Requests
    private void CleanupOutdatedPairRequests(GagspeakDbContext dbContext)
    {
        var expiredRequests = dbContext.PairRequests.Where(r => r.CreationTime < DateTime.UtcNow - TimeSpan.FromDays(3)).ToList();
        if (expiredRequests.Count > 0)
        {
            dbContext.PairRequests.RemoveRange(expiredRequests);
            _logger.LogInformation($"Removing [{expiredRequests.Count}] expired pair requests");
        }
    }
    #endregion

    #region Expired Account Auth Claims
    private void CleanUpTimedOutAccountAuthClaims(GagspeakDbContext dbContext)
    {
        var activeClaimEntries = dbContext.AccountClaimAuth.Include(u => u.User).Where(a => a.StartedAt != null).ToList();
        var entriesToRemove = activeClaimEntries.Where(a => a.StartedAt != null && a.StartedAt < DateTime.UtcNow - TimeSpan.FromMinutes(15)).ToList();

        // We dont want to remove the users themselves, because it would be unfair if someone spent 2 months unverified, tried to
        // verify, it failed, timed out, then they got their profile deleted.

        // Instead, just remove the unclaimed auth entries with expired times.
        // These users should be removed as their authentications have expired.
        if (entriesToRemove.Count > 0)
        {
            _logger.LogInformation($"Cleaning up [{entriesToRemove.Count}] expired account claim authentications");
            dbContext.RemoveRange(entriesToRemove);
        }
    }
    #endregion
}
