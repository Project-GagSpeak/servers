using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
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
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly ILogger<UserCleanupService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly IRedisDatabase _redis;

    public UserCleanupService(GagspeakMetrics metrics, IConfigurationService<ServerConfiguration> config,
        IDbContextFactory<GagspeakDbContext> dbContextFactory, ILogger<UserCleanupService> logger, 
        IHubContext<GagspeakHub, IGagspeakHub> hubContext, IRedisDatabase redis)
    {
        _metrics = metrics;
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
        while (!ct.IsCancellationRequested)
        {
            using (GagspeakDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false))
            {
                // Remove accounts that have been inactive longer than the configured day count.
                await PurgeUnusedAccounts(dbContext).ConfigureAwait(false);
                // Remove any VibeRooms created longer than 8 hours.
                await CleanUpOldRooms(dbContext).ConfigureAwait(false);
                // Reset the upload counters on user's for Share Hubs every week.
                await ResetUploadCounters(dbContext).ConfigureAwait(false);
                // Remove AccountAuthClaims that have been inactive longer than they should be.
                CleanUpTimedOutAccountAuthClaims(dbContext);
                
                CleanupOutdatedPairRequests(dbContext);

                dbContext.SaveChanges();
            }
            DateTime now = DateTime.Now;
            TimeOnly currentTime = new(now.Hour, now.Minute, now.Second);
            TimeOnly futureTime = new(now.Hour, now.Minute - now.Minute % 10, 0);
            TimeSpan span = futureTime.AddMinutes(10) - currentTime;

            _logger.LogInformation("User Cleanup Complete, next run at {date}", now.Add(span));
            await Task.Delay(span, ct).ConfigureAwait(false);
        }
    }

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
                // Could perform a ExecuteSqlRawAsync if we wanted, but try this out for now so it is more readable.
                .ExecuteUpdateAsync(ar => ar.SetProperty(
                    a => a.AccountRep.UploadAllowances,
                    // Check no role fist, as people will more likely match the first case.
                    a => a.User.Tier == CkSupporterTier.NoRole ? 10 :
                         a.User.Tier == CkSupporterTier.KinkporiumMistress ? 999999 :
                         a.User.Tier == CkSupporterTier.DistinguishedConnoisseur ? 20 :
                         a.User.Tier == CkSupporterTier.EsteemedPatron ? 15 :
                         a.User.Tier == CkSupporterTier.ServerBooster ? 15 :
                         12
                )).ConfigureAwait(false);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during upload counter reset");
        }
    }

    private async Task CleanUpOldRooms(GagspeakDbContext dbContext)
    {
        DateTime nowUtc = DateTime.UtcNow;
        _logger.LogInformation("Cleaning up Redis rooms older than 8 hours");

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

                // invites would get handled here, but look into if we even consider them first.
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Redis old rooms cleanup");
        }
    }

    private async Task PurgeUnusedAccounts(GagspeakDbContext dbContext)
    {
        try
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
                        await _hubContext.Clients.Users(removedPairUids).Callback_RemoveClientPair(new(new(removedUser))).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Cleaning up unauthorized users");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during user purge");
        }
    }

    private void CleanupOutdatedPairRequests(GagspeakDbContext dbContext)
    {
        try
        {
            _logger.LogInformation("Cleaning up expired pair requests");

            List<PairRequest> expiredRequests = dbContext.PairRequests.Where(r => r.CreationTime < DateTime.UtcNow - TimeSpan.FromDays(3)).ToList();
            _logger.LogInformation("Removing " + expiredRequests.Count + " expired pair requests");
            dbContext.PairRequests.RemoveRange(expiredRequests);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during pair request cleanup");
        }
    }

    private void CleanUpTimedOutAccountAuthClaims(GagspeakDbContext dbContext)
    {
        try
        {
            _logger.LogInformation($"Cleaning up expired account claim authentications");
            List<AccountClaimAuth> activeClaimEntries = dbContext.AccountClaimAuth.Include(u => u.User).Where(a => a.StartedAt != null).ToList();

            IEnumerable<AccountClaimAuth> entriesToRemove = activeClaimEntries.Where(a => a.StartedAt != null && a.StartedAt < DateTime.UtcNow - TimeSpan.FromMinutes(15));

            // We dont want to remove the users themselves, because it would be unfair if someone spent 2 months unverified, tried to
            // verify, it failed, timed out, then they got their profile deleted.

            // Instead, just remove the unclaimed auth entries with expired times.
            // These users should be removed as their authentications have expired.
            dbContext.RemoveRange(entriesToRemove);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during expired auths cleanup");
        }
    }
}
