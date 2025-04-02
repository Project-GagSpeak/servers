using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using GagspeakServer.Hubs;
using GagspeakAPI.SignalR;
using GagspeakAPI.Enums;

namespace GagspeakServer.Services;
/// <summary> Service for cleaning up users and groups that are no longer active </summary>
public class UserCleanupService : IHostedService
{
    private readonly GagspeakMetrics _metrics;
    private readonly ILogger<UserCleanupService> _logger;
    private readonly IServiceProvider _services;
    private readonly IConfigurationService<ServerConfiguration> _configuration;
    private CancellationTokenSource _cleanupCts = new();

    public UserCleanupService(GagspeakMetrics metrics, ILogger<UserCleanupService> logger,
        IServiceProvider services, IConfigurationService<ServerConfiguration> configuration)
    {
        _metrics = metrics;
        _logger = logger;
        _services = services;
        _configuration = configuration;
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
            using var scope = _services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetService<GagspeakDbContext>()!;

            await PurgeUnusedAccounts(dbContext).ConfigureAwait(false);

            await CleanUpOldRooms(dbContext).ConfigureAwait(false);

            await ResetUploadCounters(dbContext).ConfigureAwait(false);

            CleanUpOutdatedAccountAuths(dbContext);
            CleanupOutdatedPairRequests(dbContext);

            dbContext.SaveChanges();

            var now = DateTime.Now;
            TimeOnly currentTime = new(now.Hour, now.Minute, now.Second);
            TimeOnly futureTime = new(now.Hour, now.Minute - now.Minute % 10, 0);
            var span = futureTime.AddMinutes(10) - currentTime;

            _logger.LogInformation("User Cleanup Complete, next run at {date}", now.Add(span));
            await Task.Delay(span, ct).ConfigureAwait(false);
        }
    }

    private async Task ResetUploadCounters(GagspeakDbContext dbContext)
    {
        try
        {
            _logger.LogInformation("Resetting upload counters for users");

            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            var usersToReset = await dbContext.Users
                .Where(user => user.FirstUploadTimestamp != DateTime.MinValue && user.FirstUploadTimestamp >= oneWeekAgo)
                .ToListAsync().ConfigureAwait(false);

            foreach (var user in usersToReset)
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
        try
        {
            _logger.LogInformation("Cleaning up rooms older than 12 hours");

            var twelveHoursAgo = DateTime.UtcNow - TimeSpan.FromHours(12);
            var oldRooms = await dbContext.PrivateRooms
                .Where(r => r.TimeMade < twelveHoursAgo)
                .ToListAsync().ConfigureAwait(false);

            foreach (var room in oldRooms)
            {
                _logger.LogInformation("Removing room: {roomName}", room.NameID);

                var roomUsers = dbContext.PrivateRoomPairs
                    .Where(pru => pru.PrivateRoomNameID == room.NameID)
                    .ToList();

                dbContext.PrivateRoomPairs.RemoveRange(roomUsers);
                dbContext.PrivateRooms.Remove(room);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during old rooms cleanup");
        }
    }

    private async Task PurgeUnusedAccounts(GagspeakDbContext dbContext)
    {
        try
        {
            if (_configuration.GetValueOrDefault(nameof(ServerConfiguration.PurgeUnusedAccounts), false))
            {
                var usersOlderThanDays = _configuration.GetValueOrDefault(nameof(ServerConfiguration.PurgeUnusedAccountsPeriodInDays), 120);

                _logger.LogInformation("Cleaning up users older than {usersOlderThanDays} days", usersOlderThanDays);

                var allUsers = dbContext.Users.Where(u => string.IsNullOrEmpty(u.Alias)).ToList();
                List<User> usersToRemove = new();
                foreach (var user in allUsers)
                {
                    if (user.LastLoggedIn < DateTime.UtcNow - TimeSpan.FromDays(usersOlderThanDays))
                    {
                        _logger.LogInformation("User outdated: {userUID}", user.UID);
                        usersToRemove.Add(user);
                    }
                }

                foreach (var user in usersToRemove)
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

            var expiredRequests = dbContext.KinksterPairRequests.Where(r => r.CreationTime < DateTime.UtcNow - TimeSpan.FromDays(3)).ToList();
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
            var accountClaimAuths = dbContext.AccountClaimAuth.Include(u => u.User).Where(a => a.StartedAt != null).ToList();
            List<AccountClaimAuth> expiredAuths = new List<AccountClaimAuth>();
            foreach (var auth in accountClaimAuths)
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

        var claimAuth = dbContext.AccountClaimAuth.SingleOrDefault(a => a.User != null && a.User.UID == user.UID);

        if (claimAuth != null)
        {
            dbContext.Remove(claimAuth);
        }

        var auth = dbContext.Auth.Single(a => a.UserUID == user.UID);

        var ownPairData = dbContext.ClientPairs.Where(u => u.User.UID == user.UID).ToList();
        dbContext.ClientPairs.RemoveRange(ownPairData);
        var otherPairData = dbContext.ClientPairs.Include(u => u.User)
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
