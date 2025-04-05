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
            _logger.LogDebug("Located Seconday User: {uid}, Purging them first.", secondaryUser.UID);
            await PurgeUser(_logger, secondaryUser, dbContext).ConfigureAwait(false);
        }

        var accountClaim = dbContext.AccountClaimAuth.SingleOrDefault(a => a.User.UID == user.UID);
        var ownPairData = await dbContext.ClientPairs.Where(u => u.User.UID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairPermData = await dbContext.ClientPairPermissions.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairAccessData = await dbContext.ClientPairPermissionAccess.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairKinksterRequests = await dbContext.KinksterPairRequests.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var globalPerms = await dbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
/*        var appearanceData = await dbContext.UserGagData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var activeStateData = await dbContext.UserRestraintData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);*/
        var likedPatterns = await dbContext.LikesPatterns.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var likedMoodles = await dbContext.LikesMoodles.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var achievementData = await dbContext.UserAchievementData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var userProfileData = await dbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        if(accountClaim is not null) dbContext.Remove(accountClaim);
        dbContext.RemoveRange(ownPairData);
        dbContext.RemoveRange(pairPermData);
        dbContext.RemoveRange(pairAccessData);
        dbContext.RemoveRange(pairKinksterRequests);
        if(globalPerms is not null) dbContext.Remove(globalPerms);
/*        if(appearanceData is not null) dbContext.Remove(appearanceData);
        if(activeStateData is not null) dbContext.Remove(activeStateData);*/
        dbContext.RemoveRange(likedPatterns);
        dbContext.RemoveRange(likedMoodles);
        if(achievementData is not null) dbContext.Remove(achievementData);
        if(userProfileData is not null) dbContext.Remove(userProfileData);
        if(achievementData is not null) dbContext.Remove(achievementData);

        var auth = dbContext.Auth.Single(a => a.UserUID == user.UID);
        _logger.LogInformation("User purged: {uid}", user.UID);

        dbContext.Auth.Remove(auth);
        dbContext.Users.Remove(user);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}