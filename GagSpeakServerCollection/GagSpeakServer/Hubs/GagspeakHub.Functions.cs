using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0016, CS8619
#nullable enable
public partial class GagspeakHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");
    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");
    public string UserHasTempAccess => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal))?.Value ?? throw new Exception("No TempAccess in Claims");

    /// <summary>
    ///     Gets all unpaused pairs of <paramref name="uid"/>
    /// </summary>
    private async Task<List<string>> GetAllPairedUnpausedUsers(string? uid = null)
    {
        // if no UID is provided, set it to the client caller context UserUID
        uid ??= UserUID;
        // return the list of UID's of all users that are paired with the provided UID
        return (await GetUnpausedOnlinePairs(uid).ConfigureAwait(false));
    }

    /// <summary> Helper to get the total number of users who are online currently from the list of passed in UID's.</summary>
    private async Task<Dictionary<string, string>> GetOnlineUsers(List<string> uids)
    {
        var result = await _redis.GetAllAsync<string>(uids.Select(u => "GagspeakHub:UID:" + u).ToHashSet(StringComparer.Ordinal)).ConfigureAwait(false);
        return uids.Where(u => result.TryGetValue("GagspeakHub:UID:" + u, out var ident) && !string.IsNullOrEmpty(ident)).ToDictionary(u => u, u => result["GagspeakHub:UID:" + u], StringComparer.Ordinal);
    }

    /// <summary> Helper function to get the user's identity from the redis by their UID </summary>
    private async Task<string?> GetUserIdent(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return string.Empty;
        return await _redis.GetAsync<string>("GagspeakHub:UID:" + uid).ConfigureAwait(false);
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
    ///     Retrieves the UserInfo record outlining essential information about the connection 
    ///     of two paired individuals. (Does not care if paused or not) <para />
    ///     It is a chonky database query.
    /// </summary>
    /// <returns> the UserInfo record containing info of the connection between the two individuals. Null if not paired. </returns>
    private async Task<UserInfo?> GetPairInfo(string uid, string otheruid)
    {
        /* I will do my best to logically explain this */

        // collect row(s) where userUID == uid && otherUserUID == otheruid (( should always be true since we added just prior ))
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid && u.OtherUserUID == otheruid)
                          // join the cp2 table which is defined by the collected row(s) where userUID == otheruid && otherUserUID == uid
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid && u.UserUID == otheruid)
                              on new { cp.UserUID, cp.OtherUserUID }
                              equals new { UserUID = cp2.OtherUserUID, OtherUserUID = cp2.UserUID } into joined 
                          from c in joined.DefaultIfEmpty() // Therefore, we can use this result to determine if we are synced.
                          where cp.UserUID == uid // ((((Ensure we are only working with the given uid while doing this btw.))))
                          // and now we can make a new object with the results of this query.
                          select new //  [ Ultimately stored into clientPairs ]
                          {
                              UserUID = cp.UserUID,             // Select UserUID
                              OtherUserUID = cp.OtherUserUID,   // Select OtherUserUID
                              InitializedAt = cp.CreatedAt,     // When the pairing was created
                          };

        if (!clientPairs.Any())
            return null;

        // Start by selecting from a previously defined collection 'clientPairs'
        var resultingInfo = from user in clientPairs
                                // Join with the Users table to get details of the "other user" in each pair
                            join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID
                            // Attempt to join with the ClientPairPermissions table to find permissions the user set for the other user
                            join o in DbContext.PairPermissions.AsNoTracking().Where(u => u.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = o.UserUID, OtherUserUID = o.OtherUserUID } into ownperms
                            // find perms that uid has set for other user in the pair. Groups results into 'ownperms'
                            from ownperm in ownperms.DefaultIfEmpty()
                                // Similar to the previous join, but for a different table: ClientPairPermissionAccess
                            join oa in DbContext.PairAccess.AsNoTracking().Where(a => a.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = oa.UserUID, OtherUserUID = oa.OtherUserUID } into ownaccesses
                            // find perms that uid has set for other user in the pair. Groups results into 'ownaccesses'
                            from ownaccess in ownaccesses.DefaultIfEmpty()
                                // Now, attempt to find permissions set by the other user for the main user
                            join p in DbContext.PairPermissions.AsNoTracking().Where(u => u.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = p.UserUID, OtherUserUID = p.OtherUserUID } into otherperms
                            // find perms that the other user has set for the main user. Groups results into 'otherperms'
                            from otherperm in otherperms.DefaultIfEmpty()
                                // Similar to previous joins, but for access permissions set by the other user for the main user
                            join pa in DbContext.PairAccess.AsNoTracking().Where(a => a.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = pa.UserUID, OtherUserUID = pa.OtherUserUID } into otheraccesses
                            // find perms that the other user has set for the main user. Groups results into 'otheraccesses'
                            from otheraccess in otheraccesses.DefaultIfEmpty()
                                // Join for GlobalPerms for the main user
                            join ug in DbContext.GlobalPermissions.AsNoTracking() on user.UserUID equals ug.UserUID into userGlobalPerms
                            from userGlobalPerm in userGlobalPerms.DefaultIfEmpty()
                                // Join for GlobalPerms for the other user
                            join oug in DbContext.GlobalPermissions.AsNoTracking() on user.OtherUserUID equals oug.UserUID into otherUserGlobalPerms
                            from otherUserGlobalPerm in otherUserGlobalPerms.DefaultIfEmpty()
                                // Join for HardcoreState for the main user.
                            join uhc in DbContext.HardcoreState.AsNoTracking() on user.UserUID equals uhc.UserUID into userHardcoreState
                            from userHardcore in userHardcoreState.DefaultIfEmpty()
                                // Join for HardcoreState for the other user.
                            join ohs in DbContext.HardcoreState.AsNoTracking() on user.OtherUserUID equals ohs.UserUID into otherUserHardcoreState
                            from otherUserHcState in otherUserHardcoreState.DefaultIfEmpty()
                            // Filter to include only pairs where the main user is involved
                            where user.UserUID == uid && u.UID == user.OtherUserUID
                            // Select the final projection of data to include in the results
                            select new
                            {
                                UserUID = user.UserUID,
                                OtherUserUID = user.OtherUserUID,
                                OtherUserAlias = u.Alias,
                                OtherUserTier = u.Tier,
                                OtherUserCreatedDate = u.CreatedAt,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHardcore,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess,
                                InitializedAt = user.InitializedAt
                            };

        // Aquire the query result and form it into an established list
        var resultList = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);
        if (resultList.Count == 0)
            return null;

        // return the proper object
        return new UserInfo(
            resultList[0].OtherUserAlias,
            resultList[0].OtherUserTier,
            resultList[0].OtherUserCreatedDate,
            resultList[0].OwnGlobalPerms,
            resultList[0].OwnHardcoreState,
            resultList[0].OwnPermissions,
            resultList[0].OwnPermissionsAccess,
            resultList[0].OtherGlobalPerms,
            resultList[0].OtherHardcoreState,
            resultList[0].OtherPermissions,
            resultList[0].OtherPermissionsAccess,
            resultList[0].InitializedAt
        );
    }


    /// <summary>
    ///     Helper function to retrieve the UserInfo's for ALL pairs of a specific UID. <para />
    ///     Does not care about pause status.
    /// </summary>
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
                                InitializedAt = cp.CreatedAt    // When pairing was created.
                            };

        // Obtain the permission info for these pairs.
        // each 'clientPairs' item is the final resulting query of { UserUID, OtherUserUID }
        var resultingInfo = from user in clientPairs // <-- Define the above query as 'user'
                            // Join with the Users table to get details of the "other user" in each pair
                            join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID

                            // Attempt to join with the ClientPairPermissions table to find permissions the user set for the other user
                            join o in DbContext.PairPermissions.AsNoTracking().Where(u => u.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = o.UserUID, OtherUserUID = o.OtherUserUID } into ownperms
                            // find perms that uid has set for other user in the pair. Groups results into 'ownperms'
                            from ownperm in ownperms.DefaultIfEmpty()

                            join oa in DbContext.PairAccess.AsNoTracking().Where(a => a.UserUID == uid)
                                on new { UserUID = user.UserUID, OtherUserUID = user.OtherUserUID }
                                equals new { UserUID = oa.UserUID, OtherUserUID = oa.OtherUserUID } into ownaccesses
                            from ownaccess in ownaccesses.DefaultIfEmpty()

                            // Now, attempt to find permissions set by the other user for the main user
                            join p in DbContext.PairPermissions.AsNoTracking().Where(u => u.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = p.UserUID, OtherUserUID = p.OtherUserUID } into otherperms
                            // find perms that the other user has set for the main user. Groups results into 'otherperms'
                            from otherperm in otherperms.DefaultIfEmpty()

                            join pa in DbContext.PairAccess.AsNoTracking().Where(a => a.OtherUserUID == uid)
                                on new { UserUID = user.OtherUserUID, OtherUserUID = user.UserUID }
                                equals new { UserUID = pa.UserUID, OtherUserUID = pa.OtherUserUID } into otheraccesses
                            from otheraccess in otheraccesses.DefaultIfEmpty()

                            // Join for GlobalPerms for the main user
                            join ug in DbContext.GlobalPermissions.AsNoTracking() on user.UserUID equals ug.UserUID into userGlobalPerms
                            from userGlobalPerm in userGlobalPerms.DefaultIfEmpty()
                            
                            // Join for GlobalPerms for the other user
                            join oug in DbContext.GlobalPermissions.AsNoTracking() on user.OtherUserUID equals oug.UserUID into otherUserGlobalPerms
                            from otherUserGlobalPerm in otherUserGlobalPerms.DefaultIfEmpty()
                            
                            // Join for HardcoreState for the main user
                            join uhs in DbContext.HardcoreState.AsNoTracking() on user.UserUID equals uhs.UserUID into userHardcoreStates
                            from userHcState in userHardcoreStates.DefaultIfEmpty()

                            // Join for HardcoreState for the other user
                            join ohs in DbContext.HardcoreState.AsNoTracking() on user.OtherUserUID equals ohs.UserUID into otherUserHardcoreStates
                            from otherUserHcState in otherUserHardcoreStates.DefaultIfEmpty()

                                // Filter to include only pairs where the main user is involved
                            where user.UserUID == uid && u.UID == user.OtherUserUID
                            // Select the final projection of data to include in the results
                            select new
                            {
                                UserUID = user.UserUID,
                                OtherUserUID = user.OtherUserUID,
                                OtherUserAlias = u.Alias,
                                OtherUserSupporterTier = u.Tier,
                                OtherUserCreatedDate = u.CreatedAt,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHcState,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess,
                                InitializedAt = user.InitializedAt
                            };

        // obtain the query result and form it into an established list
        var resultList = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);

        // Group results by OtherUserUID and convert to dictionary for return
        return resultList.GroupBy(g => g.OtherUserUID, StringComparer.Ordinal).ToDictionary(g => g.Key, g =>
        {
            // for some unexplainable reason, putting a return where the var is makes this no longer work.
            // Idk why, it just doesnt.
            UserInfo userInfo = new UserInfo(
                g.First().OtherUserAlias,
                g.First().OtherUserSupporterTier,
                g.First().OtherUserCreatedDate,
                g.First().OwnGlobalPerms,
                g.First().OwnHardcoreState,
                g.First().OwnPermissions,
                g.First().OwnPermissionsAccess,
                g.First().OtherGlobalPerms,
                g.First().OtherHardcoreState,
                g.First().OtherPermissions,
                g.First().OtherPermissionsAccess,
                g.First().InitializedAt
            );
            return userInfo;
        }, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Given a defined uid, retrieve all of this uid's paired users uid's who are online and not paused. <br/>
    ///     Thankfully this query is simpler than the above as IsPaused does not exist anymore.
    /// </summary>
    private async Task<List<string>> GetUnpausedOnlinePairs(string uid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid)
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid)
                            on new { cp.UserUID, cp.OtherUserUID }
                            equals new { UserUID = cp2.OtherUserUID, OtherUserUID = cp2.UserUID }
                          where cp.UserUID == uid
                          select cp.OtherUserUID;
        // Start by selecting from a previously defined collection 'clientPairs'
        IQueryable<string> resultingInfo = 
            from otherUid in clientPairs
            join u in DbContext.Users.AsNoTracking()
                on otherUid equals u.UID
                select otherUid;
        // return the distinct list OtherUserUID's that are unpaused and online for the client caller's list of client pairs.
        return await resultingInfo.Distinct().AsNoTracking().ToListAsync().ConfigureAwait(false);
    }

    // Big record, much information of kinkster pairing.
    public record UserInfo(string Alias, CkSupporterTier Tier, DateTime Created,
        GlobalPermissions OwnGlobals, HardcoreState OwnHcState, PairPermissions OwnPerms, PairPermissionAccess OwnAccess,
        GlobalPermissions OtherGlobals, HardcoreState OtherHcState, PairPermissions OtherPerms, PairPermissionAccess OtherAccess,
        DateTime PairInitAt);
}
#pragma warning restore MA0016
#nullable disable
