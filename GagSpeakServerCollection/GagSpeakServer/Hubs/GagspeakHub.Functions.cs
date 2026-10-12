using GagspeakAPI.User;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0016, CS8619
#nullable enable
public partial class GagspeakHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");
    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");
    public string UserHasTempAccess => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal))?.Value ?? throw new Exception("No TempAccess in Claims");

    private async Task<Dictionary<string, string>> GetOnlineUsers(IEnumerable<string> uids)
    {
        var result = await _redis.GetAllAsync<string>(uids.Select(u => "UID:" + u).ToHashSet(StringComparer.Ordinal)).ConfigureAwait(false);
        return uids.Where(u => result.TryGetValue("UID:" + u, out var ident) && !string.IsNullOrEmpty(ident)).ToDictionary(u => u, u => result["UID:" + u], StringComparer.Ordinal);
    }

    /// <summary> Aquire the RedisValueString from the corrisponding UID key, if present. </summary>
    private async Task<string?> GetUserIdent(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return string.Empty;
        return await _redis.GetAsync<string>("UID:" + uid).ConfigureAwait(false);
    }

    /// <summary> Removes a Users UID, and with it their HashedContentID, from the redis database. </summary>
    private async Task RemoveUserFromRedis()
        => await _redis.RemoveAsync("UID:" + UserUID, CommandFlags.FireAndForget).ConfigureAwait(false);

    /// <summary> Updates the caller Users HashedContentID on redis. </summary>
    private async Task UpdateUserOnRedis()
        => await _redis.AddAsync("UID:" + UserUID, UserCharaIdent, TimeSpan.FromSeconds(60), When.Always, CommandFlags.FireAndForget).ConfigureAwait(false);

    /// <summary> Sends <see cref="Callback_KinksterOffline"/> containing the caller, to all online pairs of the caller. </summary>
    private async Task<List<string>> SendOfflineToKinksterPairs()
    {
        var pairedUids = await GetAllPairedUsers().ConfigureAwait(false);
        await Clients.Users(pairedUids).Callback_KinksterOffline(new(new(UserUID))).ConfigureAwait(false);
        return pairedUids;
    }

    /// <summary> Sends <see cref="Callback_KinksterOnline"/> containing the caller, to all online pairs of the caller. </summary>
    private async Task<List<string>> SendOnlineToKinksterPairs()
    {
        // This will become inaccurate once we start abstracting what a 'pair' even is.
        var pairedUids = await GetAllPairedUsers().ConfigureAwait(false);
        _logger.LogMessage($"{UserUID} Sending online to {pairedUids.Count} paired users.");
        var self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(pairedUids).Callback_KinksterOnline(new(self.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        return pairedUids;
    }

    /// <summary> Invoked by a DbListenerNotification when a new AccountClaimAuth entry is added. </summary>
    /// <remarks> Displays the respective verification code to those logged in users, given they are connected. </remarks>
    public async Task DisplayVerificationCodes(List<AccountClaimAuth> newClaimAuths)
    {
        _logger.LogMessage("Displaying verification codes to users");
        foreach (var claimAuth in newClaimAuths)
        {
            // Identify the auth with the patching hashed key.
            var auth = await DbContext.Auth.AsNoTracking().SingleAsync(u => u.HashedKey == claimAuth.InitialGeneratedKey).ConfigureAwait(false);
            if (_userConnections.ContainsKey(auth.UserUID))
                await Clients.User(auth.UserUID).Callback_ShowVerification(new() { Code = claimAuth.VerificationCode ?? "" }).ConfigureAwait(false);
        }
    }

    /// <summary> 
    ///   Retrieves the UserInfo record outlining essential information about the connection 
    ///   of two paired individuals. (Does not care if paused or not), It's a chonky database query.
    /// </summary>
    /// <returns> the UserInfo record containing info of the connection between the two individuals. Null if not paired. </returns>
    private async Task<UserInfo?> GetPairInfo(string uid, string otheruid)
    {
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
                              TempAccepter = cp.TempAccepterUID,// If temporary, who accepted it.
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
                                OtherUID = user.OtherUserUID,
                                OtherAlias = u.Alias,
                                OtherDispName = u.DisplayName,
                                OtherColor = u.NameColor,
                                OtherGlow = u.NameGlowColor,
                                OtherTier = u.Tier,
                                OtherCreatedDate = u.CreatedAt,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHardcore,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess,
                                InitializedAt = user.InitializedAt,
                                TempAccepter = user.TempAccepter,
                            };

        // Aquire the query result and form it into an established list
        var res = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);
        if (res.Count == 0)
            return null;

        var resUserDat = new UserData(
            res[0].OtherUID,
            res[0].OtherAlias,
            res[0].OtherDispName,
            res[0].OtherColor,
            res[0].OtherGlow,
            res[0].OtherTier,
            res[0].OtherCreatedDate
        );

        // return the proper object
        return new UserInfo(
            resUserDat,
            res[0].OwnGlobalPerms,
            res[0].OwnHardcoreState,
            res[0].OwnPermissions,
            res[0].OwnPermissionsAccess,
            res[0].OtherGlobalPerms,
            res[0].OtherHardcoreState,
            res[0].OtherPermissions,
            res[0].OtherPermissionsAccess,
            res[0].InitializedAt,
            res[0].TempAccepter
        );
    }

    /// <summary>
    ///   Helper function to retrieve the UserInfo's for ALL pairs of a specific UID. <para />
    ///   Does not care about pause status.
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
                                InitializedAt = cp.CreatedAt,     // When the pairing was created
                                TempAccepter = cp.TempAccepterUID,// If temporary, who accepted it.
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
                                OtherAlias = u.Alias,
                                OtherDispName = u.DisplayName,
                                OtherColor = u.NameColor,
                                OtherGlow = u.NameGlowColor,
                                OtherTier = u.Tier,
                                OtherCreatedDate = u.CreatedAt,
                                OwnGlobalPerms = userGlobalPerm,
                                OwnHardcoreState = userHcState,
                                OwnPermissions = ownperm,
                                OwnPermissionsAccess = ownaccess,
                                OtherGlobalPerms = otherUserGlobalPerm,
                                OtherHardcoreState = otherUserHcState,
                                OtherPermissions = otherperm,
                                OtherPermissionsAccess = otheraccess,
                                InitializedAt = user.InitializedAt,
                                TempAccepter = user.TempAccepter,
                            };

        // obtain the query result and form it into an established list
        var res = await resultingInfo.AsNoTracking().ToListAsync().ConfigureAwait(false);
        return res
            .GroupBy(g => g.OtherUserUID, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g =>
            {
                var first = g.First();
                var userData = new UserData(
                    first.OtherUserUID,
                    first.OtherAlias,
                    first.OtherDispName,
                    first.OtherColor,
                    first.OtherGlow,
                    first.OtherTier,
                    first.OtherCreatedDate
                );
                var userInfo = new UserInfo(
                    userData,
                    first.OwnGlobalPerms,
                    first.OwnHardcoreState,
                    first.OwnPermissions,
                    first.OwnPermissionsAccess,
                    first.OtherGlobalPerms,
                    first.OtherHardcoreState,
                    first.OtherPermissions,
                    first.OtherPermissionsAccess,
                    first.InitializedAt,
                    first.TempAccepter
                );
                return userInfo;
            }, StringComparer.Ordinal);
    }

    public async Task<Dictionary<string, RequestInfo>> GetRespondingRequests(IEnumerable<string> uids)
    {
        var targets = uids.ToHashSet(StringComparer.Ordinal);
        if (targets.Count == 0)
            return new Dictionary<string, RequestInfo>(StringComparer.Ordinal);

        var info = from r in DbContext.PairRequests.AsNoTracking().Where(r => r.OtherUserUID == UserUID && targets.Contains(r.UserUID))
                   join sender in DbContext.Users.AsNoTracking()
                       on r.UserUID equals sender.UID
                   join senderGlobals in DbContext.GlobalPermissions.AsNoTracking()
                       on r.UserUID equals senderGlobals.UserUID
                   join targetGlobals in DbContext.GlobalPermissions.AsNoTracking()
                       on UserUID equals targetGlobals.UserUID
                   join senderHc in DbContext.HardcoreState.AsNoTracking()
                       on r.UserUID equals senderHc.UserUID
                   join targetHc in DbContext.HardcoreState.AsNoTracking()
                       on UserUID equals targetHc.UserUID
                   select new
                   {
                       TargetUid = r.UserUID,
                       Sender = sender,
                       Request = r,
                       SenderGlobals = senderGlobals,
                       SenderHC = senderHc,
                       TargetGlobals = targetGlobals,
                       TargetHC = targetHc
                   };

        var infoResult = await info.ToListAsync().ConfigureAwait(false);

        // Might need to reformat to work more like below.
        return infoResult.ToDictionary(x => x.TargetUid,
            x => new RequestInfo(x.Request, x.Sender, x.SenderGlobals, x.SenderHC, x.TargetGlobals, x.TargetHC), StringComparer.Ordinal);
    }

    private async Task<List<User>> GetRequestableUsers(IEnumerable<string> userUids)
    {
        // convert to hashset for quick matching.
        var targets = userUids.ToHashSet(StringComparer.Ordinal);
        if (targets.Count is 0)
            return [];

        var query = from u in DbContext.Users.AsNoTracking()
                    where targets.Contains(u.UID) || (u.Alias != null && targets.Contains(u.Alias))
                    // Deny if exists
                    where !DbContext.PairRequests.AsNoTracking().Any(r => (r.UserUID == UserUID && r.OtherUserUID == u.UID) || (r.UserUID == u.UID && r.OtherUserUID == UserUID))
                    // Deny if paired
                    where !DbContext.ClientPairs.AsNoTracking().Any(p => (p.UserUID == UserUID && p.OtherUserUID == u.UID) || (p.UserUID == u.UID && p.OtherUserUID == UserUID))
                    select u;
        // ret the query result as a list.
        return await query.ToListAsync().ConfigureAwait(false);
    }

    private async Task<Dictionary<string, PairingRequest>> GetSentRequests(IEnumerable<string> uids)
    {
        var targets = uids.ToHashSet(StringComparer.Ordinal);
        // Query all existing requests in one go
        var requestsQuery = from req in DbContext.PairRequests.AsNoTracking()
                            join tgt in DbContext.Users.AsNoTracking()
                            on req.OtherUserUID equals tgt.UID
                            where req.UserUID == UserUID && targets.Contains(req.OtherUserUID)
                            select new
                            {
                                Request = req,
                                TargetUid = tgt.UID,
                                TargetAlias = tgt.Alias,
                                TargetCreatedAt = tgt.CreatedAt
                            };

        var requestsList = await requestsQuery.ToListAsync().ConfigureAwait(false);

        // Form dictionary keyed by target UID
        return requestsList.ToDictionary(x => x.TargetUid, x => x.Request, StringComparer.Ordinal);
    }

    /// <inheritdoc cref="GetAllPairedUsers(string)"/>
    private async Task<List<string>> GetAllPairedUsers()
        => await GetAllPairedUsers(UserUID).ConfigureAwait(false);

    /// <summary>
    ///   Retrieves the distinct list of all UserUIDs who are direct pairs, 
    ///   or paired via a joined sanction that have InSync enabled.
    /// </summary>
    private async Task<List<string>> GetAllPairedUsers(string uid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid)
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid)
                          on new { UserUID = cp.UserUID, OtherUserUID = cp.OtherUserUID }
                          equals new { UserUID = cp2.OtherUserUID, OtherUserUID = cp2.UserUID }
                          select cp.OtherUserUID;
        // Concatinate this result, returning the distinct list of UIDs that are either direct pairs or paired via a sanction.
        return await clientPairs.Distinct().AsNoTracking().ToListAsync().ConfigureAwait(false);
    }

    public record RequestInfo(PairingRequest Request, User Sender, GlobalPermissions SenderGlobals, UserHardcoreState SenderHC, GlobalPermissions RecipientGlobals, UserHardcoreState RecipientHC);

    // Big record, much information of kinkster pairing.
    public record UserInfo(
        UserData UserData, 
        GlobalPermissions OwnGlobals,
        UserHardcoreState OwnHcState,
        PairPermissions OwnPerms,
        PairPermissionAccess OwnAccess,
        GlobalPermissions OtherGlobals,
        UserHardcoreState OtherHcState,
        PairPermissions OtherPerms,
        PairPermissionAccess OtherAccess,
        DateTime PairInitAt,
        string PairTempAccepter);
}
#pragma warning restore MA0016
#nullable disable
