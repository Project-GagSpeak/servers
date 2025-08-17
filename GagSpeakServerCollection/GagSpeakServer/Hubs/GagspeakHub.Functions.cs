using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0016
#nullable enable

public partial class GagspeakHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");
    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");
    public string UserHasTempAccess => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal))?.Value ?? throw new Exception("No TempAccess in Claims");

    /// <summary> 
    /// Helper function to remove a assist with properly deleting a user from all locations in where it was stored.
    /// </summary>
    private async Task DeleteUser(User user)
    {
        // Obtain all Auth-Related content.
        Auth auth = await DbContext.Auth.SingleAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        AccountClaimAuth? accountClaimAuth = await DbContext.AccountClaimAuth.SingleOrDefaultAsync(u => u.User != null && u.User.UID == user.UID).ConfigureAwait(false);
        // Obtain all Pair related content.
        List<ClientPair> usersKinksterPairs = await DbContext.ClientPairs.Where(u => u.User.UID == user.UID).ToListAsync().ConfigureAwait(false);
        List<ClientPairPermissions> pairPerms = await DbContext.ClientPairPermissions.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        List<ClientPairPermissionAccess> pairPermAccess = await DbContext.ClientPairPermissionAccess.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);

        // Requests
        List<KinksterRequest> kinksterRequests = await DbContext.KinksterPairRequests.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);
        List<CollarRequest> collarRequests = await DbContext.CollarRequests.Where(u => u.UserUID == user.UID || u.OtherUserUID == user.UID).ToListAsync().ConfigureAwait(false);

        // Obtain all personal data.
        UserGlobalPermissions? globals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        UserHardcoreState? hcState = await DbContext.UserHardcoreState.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        List<UserGagData> gags = await DbContext.UserGagData.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        List<UserRestrictionData> restrictions = await DbContext.UserRestrictionData.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        UserRestraintData? restraint = await DbContext.UserRestraintData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        UserCollarData? collar = await DbContext.UserCollarData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        // ShareHub info (keep uploads)
        List<LikesMoodles> likesMoodles = await DbContext.LikesMoodles.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);
        List<LikesPatterns> likesPatterns = await DbContext.LikesPatterns.Where(u => u.UserUID == user.UID).ToListAsync().ConfigureAwait(false);

        // Achievements & Profile.
        UserAchievementData? achievements = await DbContext.UserAchievementData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        UserProfileData? profile = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        // ----------------------------------------- // 
        // Remove AuthClaim is present.
        if (accountClaimAuth is not null) DbContext.AccountClaimAuth.Remove(accountClaimAuth);
        // Remove Achievements if present.
        if (achievements is not null) DbContext.UserAchievementData.Remove(achievements);
        // Remove Profile if present.
        if (profile is not null) DbContext.UserProfileData.Remove(profile);

        // Remove all requests.
        DbContext.KinksterPairRequests.RemoveRange(kinksterRequests);
        DbContext.CollarRequests.RemoveRange(collarRequests);

        // Remove all the user's pairs, and send to those pairs to remove the users.
        DbContext.ClientPairs.RemoveRange(usersKinksterPairs);
        var otherPairs = await DbContext.ClientPairs.Include(u => u.User).Where(u => u.OtherUser.UID == user.UID).AsNoTracking().ToListAsync().ConfigureAwait(false);
        foreach (var pair in otherPairs)
            await Clients.User(pair.UserUID).UserRemoveKinkster(new(user.ToUserData())).ConfigureAwait(false);

        // Remove Global-Related permissions if present.
        if (globals is not null) DbContext.UserGlobalPermissions.Remove(globals);
        // Remove Hardcore State if present.
        if (hcState is not null) DbContext.UserHardcoreState.Remove(hcState);

        // Remove stored active state data.
        DbContext.UserGagData.RemoveRange(gags);
        DbContext.UserRestrictionData.RemoveRange(restrictions);
        if (restraint is not null) DbContext.UserRestraintData.Remove(restraint);
        if (collar is not null) DbContext.UserCollarData.Remove(collar);

        // Remove all related pattern and moodle likes.
        DbContext.LikesMoodles.RemoveRange(likesMoodles);
        DbContext.LikesPatterns.RemoveRange(likesPatterns);

        // Remove the other ended client pairs
        DbContext.ClientPairs.RemoveRange(otherPairs);
        // Remove final user & auth.
        DbContext.Users.Remove(user);
        DbContext.Auth.Remove(auth);

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUsersRegisteredDeleted);
    }

    /// <summary> 
    /// 
    /// Simpler helper function that gets all paired users that are present in the client callers pair list.
    /// 
    /// </summary>
    /// <param name="uid"> The UID to search for paired users of. Can be null if none are provided.</param>
    /// <returns>
    /// A list of UID's of all users that are paired with the provided UID
    /// </returns>
    private async Task<List<string>> GetAllPairedUnpausedUsers(string? uid = null)
    {
        // if no UID is provided, set it to the client caller context UserUID
        uid ??= UserUID;
        // return the list of UID's of all users that are paired with the provided UID
        return (await GetSyncedUnpausedOnlinePairs(uid).ConfigureAwait(false));
    }

    /// <summary> Helper to get the total number of users who are online currently from the list of passed in UID's.</summary>
    private async Task<Dictionary<string, string>> GetOnlineUsers(List<string> uids)
    {
        IDictionary<string, string?> result = await _redis.GetAllAsync<string>(uids.Select(u => "GagspeakHub:UID:" + u).ToHashSet(StringComparer.Ordinal)).ConfigureAwait(false);
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return uids.Where(u => result.TryGetValue("GagspeakHub:UID:" + u, out string? ident) && !string.IsNullOrEmpty(ident)).ToDictionary(u => u, u => result["GagspeakHub:UID:" + u], StringComparer.Ordinal);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }

    /// <summary> Helper function to get the user's identity from the redis by their UID </summary>
    private async Task<string?> GetUserIdent(string uid)
    {
        if (uid.NullOrEmpty()) return string.Empty;
#pragma warning disable CS8603 // Possible null reference return.
        return await _redis.GetAsync<string>("GagspeakHub:UID:" + uid).ConfigureAwait(false);
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary> Helper function to remove a user from the redis by their UID</summary>
    private async Task RemoveUserFromRedis()
    {
        await _redis.RemoveAsync("GagspeakHub:UID:" + UserUID, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> A helper function to update the user's identity on the redi's by their UID</summary>
    private async Task UpdateUserOnRedis()
    {
        await _redis.AddAsync("GagspeakHub:UID:" + UserUID, UserCharaIdent, TimeSpan.FromSeconds(60),
            StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> 
    /// Called upon by a client whenever they logout from the servers / go offline.
    /// This call then sends to all paired users of the client who are offline, a User DTO.
    /// </summary>
    /// <returns> 
    /// A list of UID's of all users that are paired with the provided UID 
    /// </returns>
    private async Task<List<string>> SendOfflineToAllPairedUsers()
    {
        // grab all paired unpaused users, our user object, and send our offlineIdentDTO to the list of unpaused paired users.
        List<string> usersToSendDataTo = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        User self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(usersToSendDataTo).Callback_KinksterOffline(new(self.ToUserData())).ConfigureAwait(false);
        return usersToSendDataTo;
    }

    /// <summary> 
    /// Called upon by a client whenever they login to the servers / go online.
    /// This call then sends to all paired users of the client who are online, an OnlineUserIdent DTO.
    /// </summary>
    /// <returns> 
    /// A list of UID's of all users that are paired with the provided UID 
    /// </returns>
    private async Task<List<string>> SendOnlineToAllPairedUsers()
    {
        // grab all paired unpaused users, our user object, and send our onlineIdentDTO to the list of unpaused paired users.
        List<string> usersToSendDataTo = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        User self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(usersToSendDataTo).Callback_KinksterOnline(new(self.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        // return the list of UID strings that we sent the online message to.
        return usersToSendDataTo;
    }

    /// <summary> Helper function to display any added auth claims as popups to the clients user</summary>
    public async Task DisplayVerificationCodesToOnlineUsers(List<AccountClaimAuth> newlyAddedAccountClaims)
    {
        _logger.LogMessage("Displaying verification codes to users");

        // for each authentication that was newly added
        foreach (AccountClaimAuth auth in newlyAddedAccountClaims)
        {
            // locate the auth in the database with the matching hashed key
            Auth matchingUserAuth = await DbContext.Auth.AsNoTracking().SingleAsync(u => u.HashedKey == auth.InitialGeneratedKey).ConfigureAwait(false);

            // then locate the userUID of that auth object
            string? userUID = matchingUserAuth.UserUID;

            // see if that user UID is in the list of user connections
            if (userUID is not null && _userConnections.ContainsKey(userUID))
                await Clients.User(userUID).Callback_ShowVerification(new() { Code = auth.VerificationCode ?? "" }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A helper function that fetches all bidirectional (synced) client pairs for a given user.
    /// </summary>
    public async Task<List<User>> GetSyncedPairs(string uid)
    {
        // Query to find bidirectional (synced) client pairs
        IQueryable<User> syncedPairsQuery = from cp in DbContext.ClientPairs
                               join cp2 in DbContext.ClientPairs
                               on new { UserUID = cp.UserUID, OtherUserUID = cp.OtherUserUID }
                               equals new { UserUID = cp2.OtherUserUID, OtherUserUID = cp2.UserUID }
                               where cp.UserUID == uid
                               select cp.OtherUser; // Select the OtherUser object directly

        // Execute the query and return the list of User objects
        List<User> syncedUsers = await syncedPairsQuery.Distinct().ToListAsync().ConfigureAwait(false);
        return syncedUsers;
    }

    /// <summary> 
    /// A helper function to get the pair information of a user and another user from the database.
    /// This is a very detailed function, i did my best to comment its logic and how it works.
    /// </summary>
    private async Task<UserInfo?> GetPairInfo(string uid, string otheruid)
    {
        /* I will do my best to logically explain this */

        // collect row(s) where userUID == uid && otherUserUID == otheruid (( should always be true since we added just prior ))
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid && u.OtherUserUID == otheruid)
                              // join the cp2 table which is defined by the collected row(s) where userUID == otheruid && otherUserUID == uid
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid && u.UserUID == otheruid)
                          // this result is joined only when 
                          on new // this created object, aka our client pair object we made prior to calling this 
                          {
                              UserUID = cp.UserUID, // Create an anonymous object with UserUID
                              OtherUserUID = cp.OtherUserUID // and OtherUserUID
                          }
                          equals new // matches the other user's client pair object that they would have generated if they added us.
                          {
                              UserUID = cp2.OtherUserUID, // Join where the UserUID matches OtherUserUID of cp2
                              OtherUserUID = cp2.UserUID // and OtherUserUID matches UserUID of cp2
                          } into joined // if this joined variable is empty or default, it means they have not added us back, so we are not synced.
                          from c in joined.DefaultIfEmpty() // Therefore, we can use this result to determine if we are synced.
                          where cp.UserUID == uid // ((((Ensure we are only working with the given uid while doing this btw.))))
                          // and now we can make a new object with the results of this query.
                          select new //  [ Ultimately stored into clientPairs ]
                          {
                              UserUID = cp.UserUID, // Select UserUID
                              OtherUserUID = cp.OtherUserUID, // Select OtherUserUID
                              Synced = c != null // Check if the join found a match, indicating synchronization
                          };

        // Start by selecting from a previously defined collection 'clientPairs'
        var resultingInfo = from user in clientPairs
                                // Join with the Users table to get details of the "other user" in each pair
                            join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID
                            // Attempt to join with the ClientPairPermissions table to find permissions the user set for the other user
                            join o in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = o.UserUID, OtherUserUID = o.OtherUserUID } into ownperms
                            // find perms that uid has set for other user in the pair. Groups results into 'ownperms'
                            from ownperm in ownperms.DefaultIfEmpty()
                                // Similar to the previous join, but for a different table: ClientPairPermissionAccess
                            join oa in DbContext.ClientPairPermissionAccess.AsNoTracking().Where(a => a.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = oa.UserUID, OtherUserUID = oa.OtherUserUID } into ownaccesses
                            // find perms that uid has set for other user in the pair. Groups results into 'ownaccesses'
                            from ownaccess in ownaccesses.DefaultIfEmpty()
                                // Now, attempt to find permissions set by the other user for the main user
                            join p in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = p.UserUID, OtherUserUID = p.OtherUserUID } into otherperms
                            // find perms that the other user has set for the main user. Groups results into 'otherperms'
                            from otherperm in otherperms.DefaultIfEmpty()
                                // Similar to previous joins, but for access permissions set by the other user for the main user
                            join pa in DbContext.ClientPairPermissionAccess.AsNoTracking().Where(a => a.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = pa.UserUID, OtherUserUID = pa.OtherUserUID } into otheraccesses
                            // find perms that the other user has set for the main user. Groups results into 'otheraccesses'
                            from otheraccess in otheraccesses.DefaultIfEmpty()
                                // Join for GlobalPerms for the main user
                            join ug in DbContext.UserGlobalPermissions.AsNoTracking() on user.UserUID equals ug.UserUID into userGlobalPerms
                            from userGlobalPerm in userGlobalPerms.DefaultIfEmpty()
                                // Join for GlobalPerms for the other user
                            join oug in DbContext.UserGlobalPermissions.AsNoTracking() on user.OtherUserUID equals oug.UserUID into otherUserGlobalPerms
                            from otherUserGlobalPerm in otherUserGlobalPerms.DefaultIfEmpty()
                                // Join for HardcoreState for the main user.
                            join uhc in DbContext.UserHardcoreState.AsNoTracking() on user.UserUID equals uhc.UserUID into userHardcoreState
                            from userHardcore in userHardcoreState.DefaultIfEmpty()
                                // Join for HardcoreState for the other user.
                            join ohs in DbContext.UserHardcoreState.AsNoTracking() on user.OtherUserUID equals ohs.UserUID into otherUserHardcoreState
                            from otherUserHcState in otherUserHardcoreState.DefaultIfEmpty()
                            // Filter to include only pairs where the main user is involved
                            where user.UserUID == uid && u.UID == user.OtherUserUID
                            // Select the final projection of data to include in the results
                            select new
                            {
                                UserUID = user.UserUID,
                                OtherUserUID = user.OtherUserUID,
                                OtherUserAlias = u.Alias,
                                OtherUserSupporterTier = u.VanityTier,
                                OtherUserCreatedDate = u.CreatedDate,
                                Synced = user.Synced,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHardcore,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess
                            };

        // Aquire the query result and form it into an established list
        var resultList = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);

        // if its empty, return null
        if (!resultList.Any()) return null;

        // return the proper object
        return new UserInfo(
            resultList[0].OtherUserAlias, // the alias of the user.
            resultList[0].OtherUserSupporterTier,
            resultList[0].OtherUserCreatedDate,
            resultList.Max(p => p.Synced), // if they are synced.
            resultList[0].OwnGlobalPerms,
            resultList[0].OwnHardcoreState,
            resultList[0].OwnPermissions,
            resultList[0].OwnPermissionsAccess,
            resultList[0].OtherGlobalPerms,
            resultList[0].OtherHardcoreState,
            resultList[0].OtherPermissions,
            resultList[0].OtherPermissionsAccess
            );
    }


    /// <summary> A helper function to get the pair information of a user and another user from the database</summary>
    private async Task<Dictionary<string, UserInfo>> GetAllPairInfo(string uid)
    {
        // refer to GETPAIRINFO function above to see explanation of this function.
        var clientPairs =   from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid)
                            join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid)
                            on new
                            {
                                UserUID = cp.UserUID,
                                OtherUserUID = cp.OtherUserUID
                            }
                            equals new
                            {
                                UserUID = cp2.OtherUserUID,
                                OtherUserUID = cp2.UserUID
                            } into joined
                            from c in joined.DefaultIfEmpty()
                            where cp.UserUID == uid
                            select new
                            {
                                UserUID = cp.UserUID,
                                OtherUserUID = cp.OtherUserUID,
                                Synced = c != null
                            };


        // Start by selecting from a previously defined collection 'clientPairs'
        var resultingInfo = from user in clientPairs
                                // Join with the Users table to get details of the "other user" in each pair
                            join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID
                            // Attempt to join with the ClientPairPermissions table to find permissions the user set for the other user
                            join o in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = o.UserUID, OtherUserUID = o.OtherUserUID } into ownperms
                            // find perms that uid has set for other user in the pair. Groups results into 'ownperms'
                            from ownperm in ownperms.DefaultIfEmpty()
                                // Similar to the previous join, but for a different table: ClientPairPermissionAccess
                            join oa in DbContext.ClientPairPermissionAccess.AsNoTracking().Where(a => a.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = oa.UserUID, OtherUserUID = oa.OtherUserUID } into ownaccesses
                            // find perms that uid has set for other user in the pair. Groups results into 'ownaccesses'
                            from ownaccess in ownaccesses.DefaultIfEmpty()
                                // Now, attempt to find permissions set by the other user for the main user
                            join p in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = p.UserUID, OtherUserUID = p.OtherUserUID } into otherperms
                            // find perms that the other user has set for the main user. Groups results into 'otherperms'
                            from otherperm in otherperms.DefaultIfEmpty()
                                // Similar to previous joins, but for access permissions set by the other user for the main user
                            join pa in DbContext.ClientPairPermissionAccess.AsNoTracking().Where(a => a.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = pa.UserUID, OtherUserUID = pa.OtherUserUID } into otheraccesses
                            // find perms that the other user has set for the main user. Groups results into 'otheraccesses'
                            from otheraccess in otheraccesses.DefaultIfEmpty()
                                // Join for GlobalPerms for the main user
                            join ug in DbContext.UserGlobalPermissions.AsNoTracking() on user.UserUID equals ug.UserUID into userGlobalPerms
                            from userGlobalPerm in userGlobalPerms.DefaultIfEmpty()
                                // Join for GlobalPerms for the other user
                            join oug in DbContext.UserGlobalPermissions.AsNoTracking() on user.OtherUserUID equals oug.UserUID into otherUserGlobalPerms
                            from otherUserGlobalPerm in otherUserGlobalPerms.DefaultIfEmpty()
                                // Join for HardcoreState for the main user.
                            join uhs in DbContext.UserHardcoreState.AsNoTracking() on user.UserUID equals uhs.UserUID into userHardcoreState
                            from userHcState in userHardcoreState.DefaultIfEmpty()
                                // Join for HardcoreState for the other user.
                            join ohs in DbContext.UserHardcoreState.AsNoTracking() on user.OtherUserUID equals ohs.UserUID into otherUserHardcoreState
                            from otherUserHcState in otherUserHardcoreState.DefaultIfEmpty()
                                // Filter to include only pairs where the main user is involved
                            where user.UserUID == uid
                                && u.UID == user.OtherUserUID
                                && ownperm.UserUID == user.UserUID && ownperm.OtherUserUID == user.OtherUserUID
                                && ownaccess.UserUID == user.UserUID && ownaccess.OtherUserUID == user.OtherUserUID
                                && (otherperm == null || (otherperm.UserUID == user.OtherUserUID && otherperm.OtherUserUID == user.UserUID))
                                && (otheraccess == null || (otheraccess.UserUID == user.OtherUserUID && otheraccess.OtherUserUID == user.UserUID))
                            // Select the final projection of data to include in the results
                            select new
                            {
                                UserUID = user.UserUID,
                                OtherUserUID = user.OtherUserUID,
                                OtherUserAlias = u.Alias,
                                OtherUserSupporterTier = u.VanityTier,
                                OtherUserCreatedDate = u.CreatedDate,
                                Synced = user.Synced,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHcState,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess
                            };

        // obtain the query result and form it into an established list
        var resultList = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);


        // Group results by OtherUserUID and convert to dictionary for return
        return resultList.GroupBy(g => g.OtherUserUID, StringComparer.Ordinal).ToDictionary(g => g.Key, g =>
        {
            // for some unexplainable reason, putting a return where the var is makes this no longer work. I dont fucking know why, it just doesnt.
            UserInfo userInfo = new UserInfo(
                g.First().OtherUserAlias, // the alias of the user.
                g.First().OtherUserSupporterTier,
                g.First().OtherUserCreatedDate,
                g.Max(p => p.Synced), // if they are synced.
                g.First().OwnGlobalPerms,
                g.First().OwnHardcoreState,
                g.First().OwnPermissions,
                g.First().OwnPermissionsAccess,
                g.First().OtherGlobalPerms,
                g.First().OtherHardcoreState,
                g.First().OtherPermissions,
                g.First().OtherPermissionsAccess
            );

            //_logger.LogWarning($"UserInfo for {g.Key}: {JsonConvert.SerializeObject(userInfo)}");
            return userInfo;
        }, StringComparer.Ordinal);
    }

    /// <summary> A helper function to get the pair information of a user and another user from the database</summary>
    private async Task<List<string>> GetSyncedUnpausedOnlinePairs(string uid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid)
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid)
                          on new
                          {
                              UserUID = cp.UserUID,
                              OtherUserUID = cp.OtherUserUID
                          }
                          equals new
                          {
                              UserUID = cp2.OtherUserUID,
                              OtherUserUID = cp2.UserUID
                          } into joined
                          from c in joined.DefaultIfEmpty()
                          where cp.UserUID == uid && c.UserUID != null
                          select new
                          {
                              UserUID = cp.UserUID,
                              OtherUserUID = cp.OtherUserUID,
                          };

        // Start by selecting from a previously defined collection 'clientPairs'
        IQueryable<string> resultingInfo = 
            from user in clientPairs
                // Join with the Users table to get details of the "other user" in each pair
            join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID
            // Attempt to join with the ClientPairPermissions table to find permissions the user set for the other user
            join o in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.UserUID == uid)
                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                equals new { UserUID = o.UserUID, OtherUserUID = o.OtherUserUID } into ownperms
            // find perms that uid has set for other user in the pair. Groups results into 'ownperms'
            from ownperm in ownperms.DefaultIfEmpty()
                // Now, attempt to find permissions set by the other user for the main user
            join p in DbContext.ClientPairPermissions.AsNoTracking().Where(u => u.OtherUserUID == uid)
                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                equals new { UserUID = p.UserUID, OtherUserUID = p.OtherUserUID } into otherperms
            // find perms that the other user has set for the main user. Groups results into 'otherperms'
            from otherperm in otherperms.DefaultIfEmpty()
                // Filter to include only pairs where the main user is involved
            where user.UserUID == uid
                && ownperm.UserUID == user.UserUID && ownperm.OtherUserUID == user.OtherUserUID
                && otherperm.OtherUserUID == user.UserUID && otherperm.UserUID == user.OtherUserUID
                && !ownperm.IsPaused && (otherperm == null ? false : !otherperm.IsPaused)
            // Select OtherUserUID for the final determining factor
            select user.OtherUserUID;

        // return the distinct list OtherUserUID's that are unpaused and online for the client caller's list of client pairs.
        return await resultingInfo.Distinct().AsNoTracking().ToListAsync().ConfigureAwait(false);
    }


    public record UserInfo(
        string Alias,
        CkSupporterTier SupporterTier,
        DateTime createdDate,
        bool IsSynced,
        UserGlobalPermissions ownGlobalPerms,
        UserHardcoreState ownHardcoreState,
        ClientPairPermissions ownPairPermissions,
        ClientPairPermissionAccess ownPairPermissionAccess,
        UserGlobalPermissions otherGlobalPerms,
        UserHardcoreState otherHardcoreState,
        ClientPairPermissions otherPairPermissions,
        ClientPairPermissionAccess otherPairPermissionAccess
        );
}
#pragma warning restore MA0016
#nullable disable
