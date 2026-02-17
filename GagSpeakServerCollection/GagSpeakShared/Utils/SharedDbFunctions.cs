using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task PurgeUserAccount(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        logger.LogInformation("Purging user account: {uid}", user.UID);
        // grab all profiles of the account.
        var altProfiles = await dbContext.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == user.UID).Select(c => c.User).ToListAsync().ConfigureAwait(false);
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
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<Dictionary<string, List<string>>> DeleteUserProfile(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        var retDict = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        // Obtain the caller's Auth entry, which contains the User entry inside.
        if (await dbContext.Auth.Include(a => a.User).SingleOrDefaultAsync(a => a.UserUID == user.UID).ConfigureAwait(false) is not { } callerAuth)
            return retDict;

        // If PrimaryUserUID is null or empty, it is the primary profile, and we should remove all alt profiles.
        if (callerAuth.PrimaryUserUID.Equals(callerAuth.UserUID))
        {
            var altProfiles = await dbContext.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == user.UID).Select(c => c.User).ToListAsync().ConfigureAwait(false);
            foreach (var altProfile in altProfiles)
            {
                var altUid = altProfile.UID;
                var pairedUids = await DeleteProfileInternal(altProfile, logger, dbContext, metrics).ConfigureAwait(false);
                retDict.Add(altUid, pairedUids);
            }
        }
        // Remove the profile.
        var mainUid = callerAuth.User.UID;
        var pairedMainUids = await DeleteProfileInternal(callerAuth.User, logger, dbContext, metrics).ConfigureAwait(false);
        retDict.Add(mainUid, pairedMainUids);

        // return the dictionary of removed profiles and their paired UID's.
        return retDict;
    }

    private static async Task<List<string>> DeleteProfileInternal(User user, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        // Account Data. (if auth fails to fetch, this should deservedly throw an exception!.
        var auth = dbContext.Auth.Single(a => a.UserUID == user.UID);
        var accountClaim = dbContext.AccountClaimAuth.SingleOrDefault(a => a.User != null && a.User.UID == user.UID);
        // Pair Data
        var ownPairData = await dbContext.ClientPairs.Where(u => u.User.UID == user.UID).ToListAsync().ConfigureAwait(false);
        var otherPairData = await dbContext.ClientPairs.Where(u => u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairPerms = await dbContext.PairPermissions.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var pairAccess = await dbContext.PairAccess.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Requests
        var pairRequests = await dbContext.PairRequests.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var collarRequests = await dbContext.CollarRequests.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Globals & State Data
        var globals = await dbContext.GlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var hcState = await dbContext.HardcoreState.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var gags = await dbContext.ActiveGagData.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var restrictions = await dbContext.ActiveRestrictionData.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var restraint = await dbContext.ActiveRestraintData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var collar = await dbContext.ActiveCollarData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        // Collar Owners.
        var collarLinks = await dbContext.CollarOwners.Where(u => u.OwnerUID == user.UID || u.CollaredUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // ShareHub
        var likesPatterns = await dbContext.LikesPatterns.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        var likesMoodles = await dbContext.LikesMoodles.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        // Profile-Related
        var achievementData = await dbContext.AchievementData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        var userProfileData = await dbContext.ProfileData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

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

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        // now that everything is finally gone, remove the auth & user.
        dbContext.Remove(auth);
        dbContext.Remove(user);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        if (metrics is not null) metrics.IncCounter(MetricsAPI.CounterUsersRegisteredDeleted);
        return pairedUids.ToList();
    }

    /// <summary>
    ///     Safely created the primary account profile for a <paramref name="user"/> with their 
    ///     associated <paramref name="rep"/> and <paramref name="auth"/> to the DB. <para />
    ///     At the same time, this method will generate entries in all other tables user-related data is necessary. <para />
    ///     This will help reduce connection bloat.
    /// </summary>
    public static async Task CreateMainProfile(User user, AccountReputation rep, Auth auth, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        logger.LogInformation($"Creating new profile for: {user.UID} (Alias: {user.Alias})");
        await dbContext.Users.AddAsync(user).ConfigureAwait(false);
        await dbContext.AccountReputation.AddAsync(rep).ConfigureAwait(false);
        await dbContext.Auth.AddAsync(auth).ConfigureAwait(false);

        // Create all other necessary tables for the user now that it is added successfully.
        await dbContext.GlobalPermissions.AddAsync(new GlobalPermissions { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.HardcoreState.AddAsync(new HardcoreState { UserUID = user.UID }).ConfigureAwait(false);

        // Add UserGagData (3 layers: 0, 1, 2)
        for (byte layer = 0; layer < 3; layer++)
            await dbContext.ActiveGagData.AddAsync(new UserGagData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        // Add UserRestrictionData (5 layers: 0, 1, 2, 3, 4)
        for (byte layer = 0; layer < 5; layer++)
            await dbContext.ActiveRestrictionData.AddAsync(new UserRestrictionData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        await dbContext.ActiveRestraintData.AddAsync(new UserRestraintData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.ActiveCollarData.AddAsync(new UserCollarData { UserUID = user.UID }).ConfigureAwait(false);

        await dbContext.ProfileData.AddAsync(new UserProfileData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.AchievementData.AddAsync(new UserAchievementData { UserUID = user.UID, Base64AchievementData = null }).ConfigureAwait(false);

        logger.LogInformation($"[User {user.UID} (Alias: {user.Alias}) <{user.Tier}>] was created along with other necessary table entries!");
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates an alt profile for an account safely, along with their associated <paramref name="auth"/> to the DB. <para />
    ///     At the same time, this method will generate entries in all other tables user-related data is necessary. <para />
    ///     This will help reduce connection bloat.
    /// </summary>
    public static async Task CreateAltProfile(User user, Auth auth, ILogger logger, GagspeakDbContext dbContext, GagspeakMetrics? metrics = null)
    {
        logger.LogInformation($"Creating new profile for: {user.UID} (Alias: {user.Alias})");
        await dbContext.Users.AddAsync(user).ConfigureAwait(false);
        await dbContext.Auth.AddAsync(auth).ConfigureAwait(false);

        // Add AccountReputation if their primaryUid is their UserUid.
        if (string.Equals(user.UID, auth.PrimaryUserUID, StringComparison.Ordinal))
            await dbContext.AccountReputation.AddAsync(new AccountReputation { UserUID = user.UID }).ConfigureAwait(false);

        // Create all other necessary tables for the user now that it is added successfully.
        await dbContext.GlobalPermissions.AddAsync(new GlobalPermissions { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.HardcoreState.AddAsync(new HardcoreState { UserUID = user.UID }).ConfigureAwait(false);

        // Add UserGagData (3 layers: 0, 1, 2)
        for (byte layer = 0; layer < 3; layer++)
            await dbContext.ActiveGagData.AddAsync(new UserGagData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        // Add UserRestrictionData (5 layers: 0, 1, 2, 3, 4)
        for (byte layer = 0; layer < 5; layer++)
            await dbContext.ActiveRestrictionData.AddAsync(new UserRestrictionData { UserUID = user.UID, Layer = layer }).ConfigureAwait(false);

        await dbContext.ActiveRestraintData.AddAsync(new UserRestraintData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.ActiveCollarData.AddAsync(new UserCollarData { UserUID = user.UID }).ConfigureAwait(false);

        await dbContext.ProfileData.AddAsync(new UserProfileData { UserUID = user.UID }).ConfigureAwait(false);
        await dbContext.AchievementData.AddAsync(new UserAchievementData { UserUID = user.UID, Base64AchievementData = null }).ConfigureAwait(false);

        logger.LogInformation($"[User {user.UID} (Alias: {user.Alias}) <{user.Tier}>] was created along with other necessary table entries!");
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}