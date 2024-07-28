using GagspeakAPI.Data;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;


/// <summary>
/// Use the same claim types as the gagspeak hub to make things easier for reconnecting clients
/// </summary>
public partial class ToyboxHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");
    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");
    public string UserHasTempAccess => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal))?.Value ?? throw new Exception("No TempAccess in Claims");

    /// <summary> Helper function to remove a user from the redis by their UID</summary>
    private async Task RemoveUserFromRedis()
    {
        await _redis.RemoveAsync("ToyboxHub:UID:" + UserUID, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> A helper function to update the user's identity on the redi's by their UID</summary>
    private async Task UpdateUserOnRedis()
    {
        await _redis.AddAsync("ToyboxHub:UID:" + UserUID, UserCharaIdent, TimeSpan.FromSeconds(60),
            StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> Helper to get the total number of users who are online currently from the list of passed in UID's.</summary>
    private async Task<List<string>> GetOnlineUsers(List<string> uids)
    {
        var result = await _redis.GetAllAsync<string>(uids.Select(u => "ToyboxHub:UID:" + u).ToHashSet(StringComparer.Ordinal)).ConfigureAwait(false);
        return uids.Where(u => result.TryGetValue("ToyboxHub:UID:" + u, out var ident) && !string.IsNullOrEmpty(ident)).ToList();
    }


    public async Task<List<UserData>> ToyboxUserGetOnlinePairs(List<string> uids)
    {
        _logger.LogCallInfo();

        // obtain a list of all the paired users who are currently online.
        List<string> pairs = await GetOnlineUsers(uids).ConfigureAwait(false);

        // send that you are online to all connected online pairs of the client caller.
        await SendOnlineToAllPairedUsers().ConfigureAwait(false);

        // then, return back to the client caller the list of all users that are online in their client pairs.
        return pairs.Select(u => new UserData(u)).ToList();
    }

    private async Task<List<string>> SendOfflineToAllPairedUsers()
    {
        // grab all paired unpaused users, our user object, and send our offlineIdentDTO to the list of unpaused paired users.
        var usersToSendDataTo = await GetSyncedUnpausedOnlinePairs(UserUID).ConfigureAwait(false);
        var self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(usersToSendDataTo).Client_ToyboxUserSendOffline(new(self.ToUserData())).ConfigureAwait(false);
        return usersToSendDataTo;
    }

    private async Task<List<string>> SendOnlineToAllPairedUsers()
    {
        // grab all paired unpaused users, our user object, and send our onlineIdentDTO to the list of unpaused paired users.
        var usersToSendDataTo = await GetSyncedUnpausedOnlinePairs(UserUID).ConfigureAwait(false);
        var self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(usersToSendDataTo).Client_ToyboxUserSendOnline(new(self.ToUserData())).ConfigureAwait(false);
        // return the list of UID strings that we sent the online message to.
        return usersToSendDataTo;
    }

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
                                // Join for UserGlobalPermissions for the main user
                            join ug in DbContext.UserGlobalPermissions.AsNoTracking() on user.UserUID equals ug.UserUID into userGlobalPerms
                            from userGlobalPerm in userGlobalPerms.DefaultIfEmpty()
                                // Join for UserGlobalPermissions for the other user
                            join oug in DbContext.UserGlobalPermissions.AsNoTracking() on user.OtherUserUID equals oug.UserUID into otherUserGlobalPerms
                            from otherUserGlobalPerm in otherUserGlobalPerms.DefaultIfEmpty()
                                // Filter to include only pairs where the main user is involved
                            where user.UserUID == uid
                                && u.UID == user.OtherUserUID
                            // Select OtherUserUID for the final determining factor
                            select user.OtherUserUID;

        // return the distinct list OtherUserUID's that are unpaused and online for the client caller's list of client pairs.
        return await resultingInfo.Distinct().AsNoTracking().ToListAsync().ConfigureAwait(false);
    }
}

