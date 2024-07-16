using GagspeakAPI.Data;
using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GagspeakServer.Hubs;

/// <summary> 
/// 
/// This partial class of the GagSpeakHub contains all the user related methods 
/// 
/// </summary>
public partial class GagspeakHub
{

    /// <summary> 
    /// 
    /// Called by a connected client who wishes to add another User as a pair.
    /// 
    /// <para>
    /// 
    /// Creates a new initial clientpair object for 2 users within the database 
    /// and returns the successful object to the clients.
    /// 
    /// </para>
    /// </summary>
    /// <param name="dto">The User Dto of the player they desire to add.</param>
    [Authorize(Policy = "Identified")]
    public async Task UserAddPair(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        /* -------------- VALIDATION -------------- */
        // don't allow adding nothing
        var uid = dto.User.UID.Trim();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID)) return;

        // grab other user, check if it exists and if a pair already exists
        var otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        if (otherUser == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, UID does not exist").ConfigureAwait(false);
            return;
        }

        // if the client caller is trying to add themselves... reject that too lmao.
        if (string.Equals(otherUser.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Im not quite sure why you are trying to pair to yourself, but please dont.").ConfigureAwait(false);
            return;
        }

        // check to see if the client caller is already paired with the user they are trying to add.
        var existingEntry =
            await DbContext.ClientPairs.AsNoTracking() // search the client pairs table in the database
                .FirstOrDefaultAsync(p =>               // for the first or default entry where the user UID matches the client caller's UID
                    p.User.UID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);

        // if that entry does exist, inform client caller they are already paired and return.
        if (existingEntry != null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, already paired").ConfigureAwait(false);
            return;
        }

        /* -------------- ACTUAL FUNCTION -------------- */
        // grab ourselves from the database (our UID is stored in the Hub.Functions.cs as UserUID)
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));

        // create a new client pair object, (this is not the DTO, just contains user and other user in pair)
        ClientPair wl = new ClientPair()
        {
            OtherUser = otherUser,
            User = user,
        };
        // add this clientpair relation to the database
        await DbContext.ClientPairs.AddAsync(wl).ConfigureAwait(false);

        /* Calls a massively NASA Tier DB function to get all user information we need from the DB at once. */
        var existingData = await GetPairInfo(UserUID, otherUser.UID).ConfigureAwait(false);


        /* --------- CREATING OR UPDATING our tables for otheruser, AND otherUser's tables for us --------- */

        // grab our own global permissions
        var globalPerms = existingData?.ownGlobalPerms;
        // if null, then table wasn't in database.
        if (globalPerms == null)
        {
            // create new permissions for backup obect
            globalPerms = new GagspeakShared.Models.UserGlobalPermissions() { User = user };

            // grab the existing Own Global Perms from DB
            var existingOwnGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == user.UID).ConfigureAwait(false);
            // If table row does not exist, add newly generated one above to database
            if (existingOwnGlobalPerms == null)
            {
                await DbContext.UserGlobalPermissions.AddAsync(globalPerms).ConfigureAwait(false);
            }
            // table row did exist, so update it.
            else
            {
                // update the global permissions with the freshly generated globalPermissions object.
                DbContext.UserGlobalPermissions.Update(existingOwnGlobalPerms);
            }
        }

        // grab our own pair permissions for the other user we're adding.
        var ownPairPermissions = existingData?.ownPairPermissions;
        // if null, then table wasn't in database.
        if (ownPairPermissions == null)
        {
            // create new permissions for backup object
            ownPairPermissions = new ClientPairPermissions() { User = user, OtherUser = otherUser };

            // grab the existing Own Pair Permissions from DB
            var existingDbPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
            // If table row does not exist, add newly generated one above to database
            if (existingDbPerms == null)
            {
                await DbContext.ClientPairPermissions.AddAsync(ownPairPermissions).ConfigureAwait(false);
            }
            // table row did exist, so update it.
            else
            {
                // update the pair permissions with the freshly generated pairPermissions object.
                DbContext.ClientPairPermissions.Update(existingDbPerms);
            }
        }

        // grab our own pair permissions access for the other user we're adding.
        var ownPairPermissionsAccess = existingData?.ownPairPermissionAccess;
        // if null, then table wasn't in database.
        if (ownPairPermissionsAccess == null)
        {
            // create new permissions for backup object
            ownPairPermissionsAccess = new ClientPairPermissionAccess() { User = user, OtherUser = otherUser };

            // grab the existing Own Pair Permissions Access from DB
            var existingDbPermsAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
            // If table row does not exist, add newly generated one above to database
            if (existingDbPermsAccess == null)
            {
                await DbContext.ClientPairPermissionAccess.AddAsync(ownPairPermissionsAccess).ConfigureAwait(false);
            }
            // table row did exist, so update it.
            else
            {
                // update the pair permissions access with the freshly generated pairPermissionsAccess object.
                DbContext.ClientPairPermissionAccess.Update(existingDbPermsAccess);
            }
        }

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        /* --------- Fetching other users PairPermissions for client caller --------- */
        // get the opposite entry of the client pair
        ClientPair otherEntry = OppositeEntry(otherUser.UID);
        var otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);

        // fetch the opposite entrys pairedPermissions for the client caller if they exist, otherwise make them null
        var otherGlobalPermissions = existingData?.otherGlobalPerms ?? null;
        var otherPermissions = existingData?.otherPairPermissions ?? null;
        var otherPermissionsAccess = existingData?.otherPairPermissionAccess ?? null;

        // grab our own permissions and other permissions and compile them into the objects meant to be attached to the userPairDto
        GagspeakAPI.Data.Permissions.UserGlobalPermissions ownGlobalPerms = globalPerms.ToApiGlobalPerms();
        GagspeakAPI.Data.Permissions.UserPairPermissions ownPairPerms = ownPairPermissions.ToApiUserPairPerms();
        GagspeakAPI.Data.Permissions.UserEditAccessPermissions ownAccessPerms = ownPairPermissionsAccess.ToApiUserPairEditAccessPerms();
        GagspeakAPI.Data.Permissions.UserGlobalPermissions otherGlobalPerms = otherGlobalPermissions.ToApiGlobalPerms();
        GagspeakAPI.Data.Permissions.UserPairPermissions otherPerms = otherPermissions.ToApiUserPairPerms();
        GagspeakAPI.Data.Permissions.UserEditAccessPermissions otherPermsAccess = otherPermissionsAccess.ToApiUserPairEditAccessPerms();

        // construct a new UserPairDto based on the response
        UserPairDto userPairResponse = new UserPairDto(
            otherUser.ToUserData(),
            otherEntry == null ? IndividualPairStatus.OneSided : IndividualPairStatus.Bidirectional,
            ownPairPerms,
            ownAccessPerms,
            otherGlobalPerms,
            otherPerms,
            otherPermsAccess
            );

        // inform the client caller's user that the pair was added successfully, to add the pair to their pair manager.
        await Clients.User(user.UID).Client_UserAddClientPair(userPairResponse).ConfigureAwait(false);

        // check if other user is online
        if (otherIdent == null || otherEntry == null) return;

        // send push with update to other user if other user is online

        // send the push update to the other user informing them to update the permissions of the client caller in bulk.
        await Clients.User(otherUser.UID)
            .Client_UserUpdateOtherAllPairPerms(new UserPairUpdateAllPermsDto(
                user.ToUserData(), ownGlobalPerms, ownPairPerms, ownAccessPerms)).ConfigureAwait(false);

        // and then also request them to update the individual pairing status.
        await Clients.User(otherUser.UID)
            .Client_UpdateUserIndividualPairStatusDto(new(user.ToUserData(), IndividualPairStatus.Bidirectional))
            .ConfigureAwait(false);

        // if both ends have not paused each other, then send the online status to both users.
        if (!ownPairPerms.IsPaused && !otherPerms.IsPaused)
        {
            await Clients.User(UserUID).Client_UserSendOnline(new(otherUser.ToUserData(), otherIdent)).ConfigureAwait(false);
            await Clients.User(otherUser.UID).Client_UserSendOnline(new(user.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        }
    }

    /// <summary> 
    /// 
    /// Called by a connected client who wishes to delete their account from the server.
    /// 
    /// <para> 
    /// 
    /// Method will remove all associated things with the user and delete their profile from 
    /// the server, along with all other profiles under their account.
    /// 
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserDelete()
    {
        _logger.LogCallInfo();

        // fetch the client callers user data from the database.
        var userEntry = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // for any other profiles registered under this account, fetch them from the database as well.
        var secondaryUsers = await DbContext.Auth.Include(u => u.User)
            .Where(u => u.PrimaryUserUID == UserUID)
            .Select(c => c.User)
            .ToListAsync().ConfigureAwait(false);
        // remove all the client callers secondary profiles, then finally, remove their primary profile. (dont through helper functions)
        foreach (var user in secondaryUsers)
        {
            await DeleteUser(user).ConfigureAwait(false);
        }
        await DeleteUser(userEntry).ConfigureAwait(false);
    }

    /// <summary> 
    /// 
    /// Requested by the client caller upon login, asking to get all current client pairs of them that are online.
    /// 
    /// </summary>
    /// <returns> The list of OnlineUserIdentDto objects for all client pairs that are currently connected. </returns>
    [Authorize(Policy = "Identified")]
    public async Task<List<OnlineUserIdentDto>> UserGetOnlinePairs()
    {
        _logger.LogCallInfo();

        // fetch all users who are paired with the requesting client caller and do not have them paused
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // obtain a list of all the paired users who are currently online.
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // send that you are online to all connected online pairs of the client caller.
        await SendOnlineToAllPairedUsers().ConfigureAwait(false);

        // then, return back to the client caller the list of all users that are online in their client pairs.
        return pairs.Select(p => new OnlineUserIdentDto(new UserData(p.Key), p.Value)).ToList();
    }


    /// <summary> 
    /// 
    /// Called by a connected client who wishes to retrieve the list of paired clients via a list of UserPairDto's.
    /// 
    /// </summary>
    /// <returns> A list of UserPair DTO's containing the client pairs  of the client caller </returns>
    [Authorize(Policy = "Identified")]
    public async Task<List<UserPairDto>> UserGetPairedClients()
    {
        _logger.LogCallInfo();

        // fetch our user from the users table via our UserUID claim
        User ClientCallerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // before we do anything, we need to validate the tables of our paired clients. So, lets first only grab our synced pairs.
        List<User> PairedUsers = await GetSyncedPairs(UserUID).ConfigureAwait(false);

        // now, let's check to see if each of these paired users have valid tables in the database. if they don't we should create them.
        foreach (var otherUser in PairedUsers)
        {
            // fetch our own global permissions
            var ownGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == UserUID).ConfigureAwait(false);
            // check if null, if so, create new ClientCaller Global Permissions & add it to the database.
            if (ownGlobalPerms == null)
            {
                _logger.LogMessage($"No GlobalPermissions TableRow was found for User: {UserUID}, creating new one.");
                ownGlobalPerms = new UserGlobalPermissions() { User = ClientCallerUser };
                await DbContext.UserGlobalPermissions.AddAsync(ownGlobalPerms).ConfigureAwait(false);
            }

            // fetch our own pair permissions for the other user
            var ownPairPermissions = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
            // check if null, if so, create new ClientCaller Pair Permissions & add it to the database.
            if (ownPairPermissions == null)
            {
                _logger.LogMessage($"No PairPermissions TableRow was found for User: {UserUID} in relation towards {otherUser.UID}, creating new one.");
                ownPairPermissions = new ClientPairPermissions() { User = ClientCallerUser, OtherUser = otherUser };
                await DbContext.ClientPairPermissions.AddAsync(ownPairPermissions).ConfigureAwait(false);
            }

            // fetch our own pair permissions access for the other user
            var ownPairPermissionAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);
            // check if null, if so, create new ClientCaller Pair Permissions Access & add it to the database.
            if (ownPairPermissionAccess == null) {
                _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {UserUID} in relation towards {otherUser.UID}, creating new one.");
                ownPairPermissionAccess = new ClientPairPermissionAccess() { User = ClientCallerUser, OtherUser = otherUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(ownPairPermissionAccess).ConfigureAwait(false);
            }

            // fetch the other users global permissions
            var otherGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Global Permissions & add it to the database.
            if (otherGlobalPerms == null) {
                _logger.LogMessage($"No GlobalPermissions TableRow was found for User: {otherUser.UID}, creating new one.");
                otherGlobalPerms = new UserGlobalPermissions() { User = otherUser };
                await DbContext.UserGlobalPermissions.AddAsync(otherGlobalPerms).ConfigureAwait(false);
            }

            // fetch the other users pair permissions
            var otherPairPermissions = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Pair Permissions & add it to the database.
            if (otherPairPermissions == null) {
                _logger.LogMessage($"No PairPermissions TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
                otherPairPermissions = new ClientPairPermissions() { User = otherUser, OtherUser = ClientCallerUser };
                await DbContext.ClientPairPermissions.AddAsync(otherPairPermissions).ConfigureAwait(false);
            }

            // fetch the other users pair permissions access
            var otherPairPermissionAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Pair Permissions Access & add it to the database.
            if (otherPairPermissionAccess == null) {
                _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
                otherPairPermissionAccess = new ClientPairPermissionAccess() { User = otherUser, OtherUser = ClientCallerUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(otherPairPermissionAccess).ConfigureAwait(false);
            }
        }
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch all the pair information of the client caller
        var pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        var userPairDtos = new List<UserPairDto>();

        // return the list of UserPair DTO's containing the paired clients of the client caller
        return pairs.Select(p =>
        {
            return new UserPairDto(new UserData(p.Key, p.Value.Alias),
                p.Value.ToIndividualPairStatus(),
                p.Value.ownPairPermissions.ToApiUserPairPerms(),
                p.Value.ownPairPermissionAccess.ToApiUserPairEditAccessPerms(),
                p.Value.otherGlobalPerms.ToApiGlobalPerms(),
                p.Value.otherPairPermissions.ToApiUserPairPerms(),
                p.Value.otherPairPermissionAccess.ToApiUserPairEditAccessPerms());
        }).ToList();
    }


    /// <summary>
    /// 
    /// Called by a connected client who wishes to retrieve the profile of another user.
    /// 
    /// </summary>
    /// <returns> The UserProfileDto of the user requested </returns>
    [Authorize(Policy = "Identified")]
    public async Task<UserProfileDto> UserGetProfile(UserDto user)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(user));

        // fetch all the paired un-paused users of the client caller
        var allUserPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        // if all user pairs do not contain the user UID of the user requested, and the user UID is not the client caller.
        if (!allUserPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            // returns to the client caller that they cannot view profile because they have been paused by the user.
            return new UserProfileDto(user.User, false, null, "Due to the pause status you cannot access this users profile.");
        }

        // otherwise we are valid to fetch the profile of the user requested, so attempt to locate the profile in the database.
        UserProfileData? data = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.User.UID).ConfigureAwait(false);

        // if the profile of the user requested is null, return a empty profile to the client caller.
        if (data == null) return new UserProfileDto(user.User, false, null, null);

        // if the profile is disabled, return a disabled profile to the client caller.
        if (data.ProfileDisabled) return new UserProfileDto(user.User, true, null, "This profile is currently disabled");

        // otherwise, it is a valid profile, so return it.
        return new UserProfileDto(user.User, false, data.Base64ProfilePic, data.UserDescription);
    }


    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character COMBINED data
    /// 
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserPushData(UserCharaCompositeDataMessageDto dto)
    {
        _logger.LogCallInfo();

        // fetch the recipient UID list from the recipient list of the dto
        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        // check to see if all the recipients are cached within the cache service. If not, then cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            // fetch all the paired users of the client caller
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            // see which of the paired users are in the recipient list, and cache them.
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataComposite(
            new OnlineUserCharaCompositeDataDto(new UserData(UserUID), dto.CompositeData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataComposite);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataCompositeTo, recipientUids.Count);
    }

    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character's IPC Data
    /// 
    /// </summary>
    public async Task UserPushDataIpc(UserCharaIpcDataMessageDto dto)
    {
        _logger.LogCallInfo();

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataIpc(
            new OnlineUserCharaIpcDataDto(new UserData(UserUID), dto.IPCData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo, recipientUids.Count);
    }

    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character's APPEARANCE Data
    /// 
    /// </summary>
    public async Task UserPushDataAppearance(UserCharaAppearanceDataMessageDto dto)
    {
        _logger.LogCallInfo();

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataAppearance(
            new OnlineUserCharaAppearanceDataDto(new UserData(UserUID), dto.AppearanceData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearance);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearanceTo, recipientUids.Count);
    }

    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character's WARDROBE Data
    /// 
    /// </summary>
    public async Task UserPushDataWardrobe(UserCharaWardrobeDataMessageDto dto)
    {
        _logger.LogCallInfo();

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataWardrobe(
            new OnlineUserCharaWardrobeDataDto(new UserData(UserUID), dto.WardrobeData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobe);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobeTo, recipientUids.Count);
    }

    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character's ALIAS Data
    /// 
    /// </summary>
    public async Task UserPushDataAlias(UserCharaAliasDataMessageDto dto)
    {
        _logger.LogCallInfo();

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataAlias(
            new OnlineUserCharaAliasDataDto(new UserData(UserUID), dto.AliasData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAlias);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAliasTo, recipientUids.Count);
    }

    /// <summary> 
    /// 
    /// Called by a connected client that desires to push the latest updates for their character's PATTERN Data
    /// 
    /// </summary>
    public async Task UserPushDataPattern(UserCharaPatternDataMessageDto dto)
    {
        _logger.LogCallInfo();

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataPattern(
            new OnlineUserCharaPatternDataDto(new UserData(UserUID), dto.PatternInfo)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataPattern);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataPatternTo, recipientUids.Count);
    }


    /// <summary> 
    /// 
    /// Called by a connected client who wishes to remove a user from their paired list.
    /// 
    /// </summary>>
    [Authorize(Policy = "Identified")]
    public async Task UserRemovePair(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Dont allow removing self
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) return;

        // See if clientPair exists at all in the database
        var callerPair = await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (callerPair == null) return;

        // Get pair info of the user we are removing
        var pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

        // remove the client pair from the database and update changes
        DbContext.ClientPairs.Remove(callerPair);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));

        // return to the client callers callback functions that we should remove them from the client callers pair manager.
        await Clients.User(UserUID).Client_UserRemoveClientPair(dto).ConfigureAwait(false);

        // If the pair was not individually paired, then we can return here.
        if (!pairData.IsSynced) return;

        // check if other user is online, if no then there is no need to do anything further
        var otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent == null) return;

        // check to see if the client caller had the user they removed paused.
        bool callerHadPaused = pairData.ownPairPermissions?.IsPaused ?? false;

        // send updated individual pair status to the other user in this case.
        await Clients.User(dto.User.UID).Client_UpdateUserIndividualPairStatusDto(new(new(UserUID), IndividualPairStatus.OneSided)).ConfigureAwait(false);

        // fetch the other pair permissions to see if they had us paused
        ClientPairPermissions? otherPairPermissions = pairData.otherPairPermissions;
        bool otherHadPaused = otherPairPermissions?.IsPaused ?? true;

        // if the either had paused, do nothing
        if (callerHadPaused && otherHadPaused) return;

        // but if not, then fetch the new pair data
        var currentPairData = await GetPairInfo(dto.User.UID, UserUID).ConfigureAwait(false);

        // if the now current pair data is no longer synced, then send offline to both ends
        await Clients.User(UserUID).Client_UserSendOffline(dto).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Client_UserSendOffline(new(new(UserUID))).ConfigureAwait(false);
    }

    /// <summary> 
    /// 
    /// Called by a connected client who wishes to set or update their profile data.
    /// 
    /// </summary>
    /// <param name="dto">the userProfile Dto, containing both the content of the profile, the and UID of the person it belongs to. </param>
    [Authorize(Policy = "Identified")]
    public async Task UserSetProfile(UserProfileDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new HubException("Cannot modify profile data for anyone but yourself");

        // fetch the existing profile data of the client from the DB
        var existingData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);

        // if the profile is disabled, return a error message to the client caller and return. (remove later maybe? idk)
        if (existingData?.ProfileDisabled ?? false)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your profile was permanently disabled and cannot be edited").ConfigureAwait(false);
            return;
        }

        // if the profile picture base64 string is not empty, then we need to validate the image.
        if (!string.IsNullOrEmpty(dto.ProfilePictureBase64))
        {
            // convert the base64 string to a byte array
            byte[] imageData = Convert.FromBase64String(dto.ProfilePictureBase64);
            // load the image into a memory stream
            using MemoryStream ms = new(imageData);
            // detect the format the image
            var format = await Image.DetectFormatAsync(ms).ConfigureAwait(false);
            // if the file format is not a png, reject the image and return.
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
            {
                // invoke a function call to the client caller containing a server message letting them know that they provided a non-png image.
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is not in PNG format").ConfigureAwait(false);
                return;
            }

            // the image is a png, load the image into a memory stream
            using var image = Image.Load<Rgba32>(imageData);

            // THIS CHECKS TO SEE IF THE IMAGE IS LARGER THAN THE PARMAETERS WE WANT, MANIPULATE THIS LATER FOR SUPPORTERS????
            if (image.Width > 256 || image.Height > 256 || (imageData.Length > 250 * 1024))
            {
                // invoke a function call to the client caller containing a server message letting them know that they provided a large image.
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is larger than 256x256 or more than 250KiB.").ConfigureAwait(false);
                return;
            }
        }

        // if the existing data is not null, then we need to update the existing data with the new data.
        if (existingData != null)
        {
            // if the incoming profile picture is empty, set it to null.
            if (string.Equals("", dto.ProfilePictureBase64, StringComparison.OrdinalIgnoreCase))
            {
                existingData.Base64ProfilePic = null;
            }
            // see if the new profile image is not null, if it is not, then update it
            else if (dto.ProfilePictureBase64 != null)
            {
                existingData.Base64ProfilePic = dto.ProfilePictureBase64;
            }
            // finally, if the description is not null, update it.
            if (dto.Description != null)
            {
                existingData.UserDescription = dto.Description;
            }
        }
        else // hitting this else means that the existing data was null, so we need to construct a new UserProfileData object for it.
        {
            // create a new UserProfileData object with the user UID of the client caller
            UserProfileData userProfileData = new()
            {
                UserUID = dto.User.UID,
                Base64ProfilePic = dto.ProfilePictureBase64 ?? null,
                UserDescription = dto.Description ?? null,
            };

            // add the userProfileData to the database.
            await DbContext.UserProfileData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch all paired users of the client caller
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // get all the online pairs of the client callers paired list 
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // invokes the Client_UserUpdateProfile method on all connected clients whose user IDs are specified
        // in the pairs collection, and sends the updated user profile information (dto.User) to the clients.
        await Clients.Users(pairs.Select(p => p.Key)).Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
        // invoke the client_userUpdateProfile method at the client caller that made the request.
        await Clients.Caller.Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
    }

    /// <summary> A small helper function to get the opposite entry of a client pair (how its viewed from the other side) </summary>
    private ClientPair OppositeEntry(string otherUID) =>
                    DbContext.ClientPairs.AsNoTracking().SingleOrDefault(w => w.User.UID == otherUID && w.OtherUser.UID == UserUID);
}