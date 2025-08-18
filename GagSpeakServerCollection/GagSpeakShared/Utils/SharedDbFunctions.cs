using GagspeakAPI.Hub;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace GagspeakShared.Utils;
#nullable enable

/// <summar> Quality of Life helper class for shared database functions </summary>
public static class SharedDbFunctions
{
    /// <summary>
    ///     Purges a user completely from the database, including all secondary users associated with them. <para />
    ///     This means if they are an alt account, or a main account, they are all removed. <para />
    ///     It is wise to only do this when banning someone. Should not be used on other things like cleanup.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when the auth is null.</exception>
    public static async Task PurgeUserAccount(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        logger.LogInformation("Purging user account: {uid}", user.UID);
        // grab all profiles of the account.
        var altProfiles = await dbContext.Auth.AsNoTracking().Include(u => u.User).Where(u => u.PrimaryUserUID == user.UID).Select(c => c.User).ToListAsync().ConfigureAwait(false);
        foreach (var userProfile in altProfiles)
        {
            logger.LogDebug($"Located Alt Profile: {userProfile.UID} (Alias: {userProfile.Alias}), Purging them first.");
            await DeleteUserProfile(userProfile, logger, dbContext, metrics).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Removes a single profile of a user from the database. <para />
    ///     <b>IF THE PROFILE BEING REMOVED IS THE PRIMARY ACCOUNT PROFILE, ALL ALT PROFILES ARE REMOVED.</b><para />
    /// </summary>
    /// <returns> a dictionary linking the removed userUID's to the list of UID's paired with them. Dictionary is used incase primary profile is removed. </returns>
    /// <exception cref="ArgumentNullException">Thrown when the auth is null.</exception>
    public static async Task<Dictionary<string, List<string>>> DeleteUserProfile(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        var retDict = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        // Obtain the caller's Auth entry, which contains the User entry inside.
        if (await dbContext.Auth.AsNoTracking().Include(a => a.User).SingleOrDefaultAsync(a => a.UserUID == user.UID).ConfigureAwait(false) is not { } callerAuth)
            return retDict;

        // If PrimaryUserUID is null or empty, it is the primary profile, and we should remove all alt profiles.
        if (string.IsNullOrEmpty(callerAuth.PrimaryUserUID))
        {
            var altProfiles = await dbContext.Auth.AsNoTracking().Include(u => u.User).Where(u => u.PrimaryUserUID == user.UID).Select(c => c.User).ToListAsync().ConfigureAwait(false);
            foreach (var altProfile in altProfiles)
            {
                var pairedUids = await DeleteProfileInternal(callerAuth.User, logger, dbContext, metrics).ConfigureAwait(false);
                retDict.Add(callerAuth.User.UID, pairedUids);
            }
        }
        // Remove the primary profile.
        var pairedMainUids = await DeleteProfileInternal(callerAuth.User, logger, dbContext, metrics).ConfigureAwait(false);
        retDict.Add(callerAuth.User.UID, pairedMainUids);

        // return the dictionary of removed profiles and their paired UID's.
        return retDict;
    }

    private static async Task<List<string>> DeleteProfileInternal(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        // Account Data. (if auth fails to fetch, this should deservedly throw an exception!.
        var auth = dbContext.Auth.SingleAsync(a => a.UserUID == user.UID).ConfigureAwait(false);
        var accountClaim = dbContext.AccountClaimAuth.AsNoTracking().SingleOrDefault(a => a.User != null && a.User.UID == user.UID);
        // Pair Data
        var ownPairData = await dbContext.ClientPairs.AsNoTracking().Where(u => u.User.UID == user.UID).ToListAsync().ConfigureAwait(false);
        var otherPairData = await dbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairPerms = await dbContext.ClientPairPermissions.AsNoTracking().Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairAccess = await dbContext.ClientPairPermissionAccess.AsNoTracking().Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Requests
        var pairRequests = await dbContext.KinksterPairRequests.AsNoTracking().Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var collarRequests = await dbContext.CollarRequests.AsNoTracking().Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Globals & State Data
        var globals = await dbContext.UserGlobalPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var hcState = await dbContext.UserHardcoreState.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var gags = await dbContext.UserGagData.AsNoTracking().Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var restrictions = await dbContext.UserRestrictionData.AsNoTracking().Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var restraint = await dbContext.UserRestraintData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var collar = await dbContext.UserCollarData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        // Collar Owners.
        var collarLinks = await dbContext.CollarOwners.AsNoTracking().Where(u => u.OwnerUID == user.UID || u.CollaredUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // ShareHub
        var likesPatterns = await dbContext.LikesPatterns.AsNoTracking().Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var likesMoodles = await dbContext.LikesMoodles.AsNoTracking().Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Profile-Related
        var achievementData = await dbContext.UserAchievementData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var userProfileData = await dbContext.UserProfileData.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        // Get Kinkster Pair List to output.
        var pairedUids = otherPairData.Select(p => p.UserUID);

        // Remove all associated.
        if (accountClaim is not null) dbContext.Remove(accountClaim);
        dbContext.RemoveRange(ownPairData);
        dbContext.RemoveRange(otherPairData);
        dbContext.RemoveRange(pairPerms);
        dbContext.RemoveRange(pairAccess);
        dbContext.RemoveRange(pairRequests);
        dbContext.RemoveRange(collarRequests);
        if (globals is not null) dbContext.Remove(globals);
        if (hcState is not null) dbContext.Remove(hcState);
        dbContext.RemoveRange(gags);
        dbContext.RemoveRange(restrictions);
        if (restraint is not null) dbContext.Remove(restraint);
        if (collar is not null) dbContext.Remove(collar);
        dbContext.RemoveRange(collarLinks);
        dbContext.RemoveRange(likesPatterns);
        dbContext.RemoveRange(likesMoodles);
        if (achievementData is not null) dbContext.Remove(achievementData);
        if (userProfileData is not null) dbContext.Remove(userProfileData);

        // now that everything is finally gone, remove the auth & user.
        dbContext.Remove(auth);
        dbContext.Remove(user);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        if (metrics is not null) metrics.IncCounter(MetricsAPI.CounterUsersRegisteredDeleted);
        return pairedUids.ToList();
    }

    /// <summary>
    ///     Safely adds a <paramref name="user"/> with their associated <paramref name="auth"/> to the DB. <para />
    ///     At the same time, this method will generate entries in all other tables user-related data is necessary. <para />
    ///     This will help reduce connection bloat.
    /// </summary>
    public static async Task CreateUser(User user, Auth auth, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        logger.LogInformation($"Creating new profile for: {user.UID} (Alias: {user.Alias})");
        await dbContext.Users.AddAsync(user).ConfigureAwait(false);
        await dbContext.Auth.AddAsync(auth).ConfigureAwait(false);

        // Create all other necessary tables for the user now that it is added successfully.
        await dbContext.UserGlobalPermissions.AddAsync(new UserGlobalPermissions { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.UserHardcoreState.AddAsync(new UserHardcoreState { UserUID = user.UID }).ConfigureAwait(false);

        // Add UserGagData (3 layers: 0, 1, 2)
        for (byte layer = 0; layer < 3; layer++)
            await dbContext.UserGagData.AddAsync(new UserGagData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        // Add UserRestrictionData (5 layers: 0, 1, 2, 3, 4)
        for (byte layer = 0; layer < 5; layer++)
            await dbContext.UserRestrictionData.AddAsync(new UserRestrictionData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        await dbContext.UserRestraintData.AddAsync(new UserRestraintData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.UserCollarData.AddAsync(new UserCollarData { UserUID = user.UID }).ConfigureAwait(false);

        await dbContext.UserProfileData.AddAsync(new UserProfileData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.UserAchievementData.AddAsync(new UserAchievementData { UserUID = user.UID, Base64AchievementData = null }).ConfigureAwait(false);

        logger.LogInformation($"[User {user.UID} (Alias: {user.Alias}) <{user.VanityTier}>] was created along with other nessisary table entries!");
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}