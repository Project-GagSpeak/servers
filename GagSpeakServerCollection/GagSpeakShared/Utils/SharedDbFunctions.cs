using GagspeakShared.Data;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GagspeakShared.Utils;

/// <summar> Quality of Life helper class for shared database functions </summary>
public static class SharedDbFunctions
{
    public static async Task PurgeUser(ILogger _logger, User user, GagspeakDbContext dbContext)
    {
        _logger.LogInformation("Purging user: {uid}", user.UID);

        var secondaryUsers = await dbContext.Auth.Include(u => u.User)
            .Where(u => u.PrimaryUserUID == user.UID).Select(c => c.User).ToListAsync().ConfigureAwait(false);

        foreach (var secondaryUser in secondaryUsers)
        {
            await PurgeUser(_logger, secondaryUser, dbContext).ConfigureAwait(false);
        }

        var accountClaim = dbContext.AccountClaimAuth.SingleOrDefault(a => a.User.UID == user.UID);

        var userProfileData = await dbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        if (accountClaim != null)
        {
            dbContext.Remove(accountClaim);
        }

        if (userProfileData != null)
        {
            dbContext.Remove(userProfileData);
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
}