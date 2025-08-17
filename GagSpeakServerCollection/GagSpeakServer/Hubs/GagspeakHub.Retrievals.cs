using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<List<OnlineKinkster>> UserGetOnlinePairs()
    {
        _logger.LogCallInfo();

        // fetch all users who are paired with the requesting client caller and do not have them paused
        List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // obtain a list of all the paired users who are currently online.
        Dictionary<string, string> pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // send that you are online to all connected online pairs of the client caller.
        await SendOnlineToAllPairedUsers().ConfigureAwait(false);

        // then, return back to the client caller the list of all users that are online in their client pairs.
        return pairs.Select(p => new OnlineKinkster(new UserData(p.Key), p.Value)).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<List<KinksterPair>> UserGetPairedClients()
    {
        //_logger.LogCallInfo();
        // --- POTENTIALLY OVERHEAD --- --- ONLY UNCOMMENT IF NEEDED ---
        //User ClientCallerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        //// before we do anything, we need to validate the tables of our paired clients. So, lets first only grab our synced pairs.
        //var PairedUsers = await GetSyncedPairs(UserUID).ConfigureAwait(false);

        //// now, let's check to see if each of these paired users have valid tables in the database. if they don't we should create them.
        //foreach (User otherUser in PairedUsers)
        //{
        //    // fetch our own global permissions
        //    UserGlobalPermissions? ownGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == UserUID).ConfigureAwait(false);
        //    // check if null, if so, create new ClientCaller Global Permissions & add it to the database.
        //    if (ownGlobalPerms is null)
        //    {
        //        _logger.LogMessage($"No GlobalPermissions TableRow was found for User: {UserUID}, creating new one.");
        //        ownGlobalPerms = new UserGlobalPermissions() { User = ClientCallerUser };
        //        await DbContext.UserGlobalPermissions.AddAsync(ownGlobalPerms).ConfigureAwait(false);
        //    }

        //    // fetch our own pair permissions for the other user
        //    ClientPairPermissions? ownPairPermissions = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
        //    // check if null, if so, create new ClientCaller Pair Permissions & add it to the database.
        //    if (ownPairPermissions is null)
        //    {
        //        _logger.LogMessage($"No PairPermissions TableRow was found for User: {UserUID} in relation towards {otherUser.UID}, creating new one.");
        //        ownPairPermissions = new ClientPairPermissions() { User = ClientCallerUser, OtherUser = otherUser };
        //        await DbContext.ClientPairPermissions.AddAsync(ownPairPermissions).ConfigureAwait(false);
        //    }

        //    // fetch our own pair permissions access for the other user
        //    ClientPairPermissionAccess? ownPairPermissionAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
        //    // check if null, if so, create new ClientCaller Pair Permissions Access & add it to the database.
        //    if (ownPairPermissionAccess is null)
        //    {
        //        _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {UserUID} in relation towards {otherUser.UID}, creating new one.");
        //        ownPairPermissionAccess = new ClientPairPermissionAccess() { User = ClientCallerUser, OtherUser = otherUser };
        //        await DbContext.ClientPairPermissionAccess.AddAsync(ownPairPermissionAccess).ConfigureAwait(false);
        //    }

        //    // fetch the other users global permissions
        //    UserGlobalPermissions? otherGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID).ConfigureAwait(false);
        //    // check if null, if so, create new OtherUser Global Permissions & add it to the database.
        //    if (otherGlobalPerms is null)
        //    {
        //        _logger.LogMessage($"No GlobalPermissions TableRow was found for User: {otherUser.UID}, creating new one.");
        //        otherGlobalPerms = new UserGlobalPermissions() { User = otherUser };
        //        await DbContext.UserGlobalPermissions.AddAsync(otherGlobalPerms).ConfigureAwait(false);
        //    }

        //    // fetch the other users hardcore state
        //    UserHardcoreState? otherHardcoreState = await DbContext.UserHardcoreState.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID).ConfigureAwait(false);
        //    // check if null, if so, create new OtherUser Hardcore State & add it to the database.
        //    if (otherHardcoreState is null)
        //    {
        //        _logger.LogMessage($"No HardcoreState TableRow was found for User: {otherUser.UID}, creating new one.");
        //        otherHardcoreState = new UserHardcoreState() { User = otherUser };
        //        await DbContext.UserHardcoreState.AddAsync(otherHardcoreState).ConfigureAwait(false);
        //    }

        //    // fetch the other users pair permissions
        //    ClientPairPermissions? otherPairPermissions = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        //    // check if null, if so, create new OtherUser Pair Permissions & add it to the database.
        //    if (otherPairPermissions is null)
        //    {
        //        _logger.LogMessage($"No PairPermissions TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
        //        otherPairPermissions = new ClientPairPermissions() { User = otherUser, OtherUser = ClientCallerUser };
        //        await DbContext.ClientPairPermissions.AddAsync(otherPairPermissions).ConfigureAwait(false);
        //    }

        //    // fetch the other users pair permissions access
        //    ClientPairPermissionAccess? otherPairPermissionAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        //    // check if null, if so, create new OtherUser Pair Permissions Access & add it to the database.
        //    if (otherPairPermissionAccess is null)
        //    {
        //        _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
        //        otherPairPermissionAccess = new ClientPairPermissionAccess() { User = otherUser, OtherUser = ClientCallerUser };
        //        await DbContext.ClientPairPermissionAccess.AddAsync(otherPairPermissionAccess).ConfigureAwait(false);
        //    }
        //}
        //await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch all the pair information of the client caller
        Dictionary<string, UserInfo> pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        // return the list of UserPair DTO's containing the paired clients of the client caller
        return pairs.Select(p =>
        {
            KinksterPair pairList = new(new UserData(p.Key, p.Value.Alias, p.Value.SupporterTier, p.Value.createdDate),
                p.Value.ownPairPermissions.ToApiKinksterPerms(),
                p.Value.ownPairPermissionAccess.ToApiKinksterEditAccess(),
                p.Value.otherGlobalPerms.ToApiGlobalPerms(),
                p.Value.otherHardcoreState.ToApiHardcoreState(),
                p.Value.otherPairPermissions.ToApiKinksterPerms(),
                p.Value.otherPairPermissionAccess.ToApiKinksterEditAccess());
            return pairList;
        }).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<ActiveRequests> UserGetActiveRequests()
    {
        // fetch all the pair requests with the UserUid in either the UserUID or OtherUserUID
        var pairRequests = await DbContext.KinksterPairRequests.Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID).ToListAsync().ConfigureAwait(false);
        var collarRequests = await DbContext.CollarRequests.Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID).ToListAsync().ConfigureAwait(false);
        // ret the requests in api format.
        return new ActiveRequests(pairRequests.Select(r => r.ToApiPairRequest()).ToList(), collarRequests.Select(r => r.ToApiCollarRequest()).ToList());
    }

    [Authorize(Policy = "Identified")]
    public async Task<KinkPlateFull> UserGetKinkPlate(KinksterBase user)
    {
        // Get list of caller's pairs.
        List<string> callerPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        // Return blank profile if it does not exist.
        if (await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.User.UID).ConfigureAwait(false) is not { } data)
            return new KinkPlateFull(user.User, new KinkPlateContent(), string.Empty);

        // If not in the callers KinksterPair list and not set to public, return nonPublic profile.
        // If requested User Profile is not in list of pairs, and is not self, return blank profile update.
        if (!callerPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal) && !data.ProfileIsPublic)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Description = "Profile is not Public!" }, string.Empty);

        // If profile is disabled / restricted, return disabled profile.
        if (data.ProfileDisabled)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Disabled = true, Description = "This profile is currently disabled" }, string.Empty);

        // It is valid, so we should grab the collar related data.

        // Return the valid profile.
        return new KinkPlateFull(user.User, data.FromProfileData(), data.Base64ProfilePic);
    }
}

