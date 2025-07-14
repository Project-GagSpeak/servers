using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Globalization;
using System.Text.Json;

namespace GagspeakServer.Services;
/// <summary> Service for cleaning up users and groups that are no longer active </summary>
public class UserCleanupService : IHostedService
{
    private readonly GagspeakMetrics _metrics;
    private readonly ILogger<UserCleanupService> _logger;
    private readonly IServiceProvider _services;
    private readonly IRedisDatabase _redis;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private CancellationTokenSource _cleanupCts = new();

    public UserCleanupService(GagspeakMetrics metrics, ILogger<UserCleanupService> logger,
        IServiceProvider services, IRedisDatabase redis, IConfigurationService<ServerConfiguration> config)
    {
        _metrics = metrics;
        _logger = logger;
        _services = services;
        _redis = redis;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleanup Service started");
        _cleanupCts = new();

        _ = CleanUp(_cleanupCts.Token);
        return Task.CompletedTask;
    }

    private async Task CleanUp(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using IServiceScope scope = _services.CreateScope();
            using GagspeakDbContext dbContext = scope.ServiceProvider.GetService<GagspeakDbContext>()!;
            
            await PurgeUnusedAccounts(dbContext).ConfigureAwait(false);

            await CleanUpOldRooms(dbContext).ConfigureAwait(false);

            await ResetUploadCounters(dbContext).ConfigureAwait(false);

            CleanUpOutdatedAccountAuths(dbContext);
            CleanupOutdatedPairRequests(dbContext);

            dbContext.SaveChanges();

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
        try
        {
            _logger.LogInformation("Resetting upload counters for users");

            DateTime oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            List<User> usersToReset = await dbContext.Users
                .Where(user => user.FirstUploadTimestamp != DateTime.MinValue && user.FirstUploadTimestamp >= oneWeekAgo)
                .ToListAsync().ConfigureAwait(false);

            foreach (User user in usersToReset)
            {
                user.UploadLimitCounter = 0;
                user.FirstUploadTimestamp = DateTime.MinValue;
                _logger.LogInformation("Reset upload counter for user: {userUID}", user.UID);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during upload counter reset");
        }
    }

    private async Task CleanUpOldRooms(GagspeakDbContext dbContext)
    {
        var nowUtc = DateTime.UtcNow;
        _logger.LogInformation("Cleaning up Redis rooms older than 8 hours");

        try
        {
            var roomKeys = await _redis.SearchKeysAsync("VibeRoom:Room:*").ConfigureAwait(false);
            foreach (var roomHashKey in roomKeys)
            {
                var roomKeyStr = roomHashKey.ToString();
                var roomName = roomKeyStr.Substring("VibeRoom:Room:".Length);

                // get the creation time of the room.
                var createdTimeValue = await _redis.Database.HashGetAsync(roomHashKey, "CreatedTimeUTC").ConfigureAwait(false);
                if (createdTimeValue.IsNullOrEmpty)
                    continue;

                // Parse and check the age
                if (!DateTime.TryParse(createdTimeValue.ToString(), null, DateTimeStyles.AdjustToUniversal, out var createdAt))
                    continue;

                if ((nowUtc - createdAt).TotalHours < 8)
                    continue;

                // --- Cleanup logic ---
                // Begin by removing all participants from the room.
                var participantsKey = VibeRoomRedis.ParticipantsKey(roomName);
                var participantUids = await _redis.Database.SetMembersAsync(participantsKey).ConfigureAwait(false);
                await _redis.Database.KeyDeleteAsync(participantsKey).ConfigureAwait(false);

                // Remove each participant’s data
                foreach (var uid in participantUids)
                {
                    var participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, uid!);
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
                var tagJson = await _redis.Database.HashGetAsync(roomHashKey, "Tags").ConfigureAwait(false);
                if (!tagJson.IsNullOrEmpty && JsonSerializer.Deserialize<List<string>>(tagJson) is { } roomTags)
                {
                    // Remove the room from the tag indexes.
                    foreach (var tag in roomTags)
                    {
                        var tagKey = VibeRoomRedis.TagIndexKey(tag);
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

                List<User> allUsers = dbContext.Users.Where(u => string.IsNullOrEmpty(u.Alias)).ToList();
                List<User> usersToRemove = new();
                foreach (User user in allUsers)
                {
                    if (user.LastLoggedIn < DateTime.UtcNow - TimeSpan.FromDays(usersOlderThanDays))
                    {
                        _logger.LogInformation("User outdated: {userUID}", user.UID);
                        usersToRemove.Add(user);
                    }
                }

                foreach (User user in usersToRemove)
                {
                    await SharedDbFunctions.PurgeUser(_logger, user, dbContext).ConfigureAwait(false);
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

            List<KinksterRequest> expiredRequests = dbContext.KinksterPairRequests.Where(r => r.CreationTime < DateTime.UtcNow - TimeSpan.FromDays(3)).ToList();
            _logger.LogInformation("Removing " + expiredRequests.Count + " expired pair requests");
            dbContext.KinksterPairRequests.RemoveRange(expiredRequests);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during pair request cleanup");
        }
    }

    private void CleanUpOutdatedAccountAuths(GagspeakDbContext dbContext)
    {
        try
        {
            _logger.LogInformation($"Cleaning up expired account claim authentications");
            List<AccountClaimAuth> accountClaimAuths = dbContext.AccountClaimAuth.Include(u => u.User).Where(a => a.StartedAt != null).ToList();
            List<AccountClaimAuth> expiredAuths = new List<AccountClaimAuth>();
            foreach (AccountClaimAuth auth in accountClaimAuths)
            {
                if (auth.StartedAt < DateTime.UtcNow - TimeSpan.FromMinutes(15))
                {
                    expiredAuths.Add(auth);
                }
            }

            // collect the list of users that have expired authentications that are not null.
            dbContext.Users.RemoveRange(expiredAuths.Where(u => u.User != null).Select(a => a.User!));
            dbContext.RemoveRange(expiredAuths);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during expired auths cleanup");
        }
    }

    public async Task PurgeUser(User user, GagspeakDbContext dbContext)
    {
        _logger.LogInformation("Purging user: {uid}", user.UID);

        AccountClaimAuth claimAuth = dbContext.AccountClaimAuth.SingleOrDefault(a => a.User != null && a.User.UID == user.UID);

        if (claimAuth != null)
        {
            dbContext.Remove(claimAuth);
        }

        Auth auth = dbContext.Auth.Single(a => a.UserUID == user.UID);

        List<ClientPair> ownPairData = dbContext.ClientPairs.Where(u => u.User.UID == user.UID).ToList();
        dbContext.ClientPairs.RemoveRange(ownPairData);
        List<ClientPair> otherPairData = dbContext.ClientPairs.Include(u => u.User)
            .Where(u => u.OtherUser.UID == user.UID).ToList();
        dbContext.ClientPairs.RemoveRange(otherPairData);

        _logger.LogInformation("User purged: {uid}", user.UID);

        dbContext.Auth.Remove(auth);
        dbContext.Users.Remove(user);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cleanupCts.Cancel();
        return Task.CompletedTask;
    }
}
