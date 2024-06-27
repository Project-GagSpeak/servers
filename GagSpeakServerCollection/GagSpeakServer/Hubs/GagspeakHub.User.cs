using Gagspeak.API.Data;
using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto.User;
using GagSpeak.API.Dto.Connection;
using GagSpeak.API.Dto.Permissions;
using GagSpeak.API.Dto.UserPair;
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
        // append it to the database
        await DbContext.ClientPairs.AddAsync(wl).ConfigureAwait(false);

        /* Calls a massively NASA Tier DB function to get all user information we need from the DB at once. */
        var existingData = await GetPairInfo(UserUID, otherUser.UID).ConfigureAwait(false);

        /* --------- Fetching client caller's own PairPermissions for other user --------- */
        var globalPerms = existingData?.ownGlobalPerms;
        // if the global permissions don't exist, aka we dont have any permissions stored for this person yet.
        if (globalPerms != null)
        {
            // create our own permissions.  generate new permissions.
            globalPerms = new GagspeakShared.Models.UserGlobalPermissions() { User = user };

            // grab the existing permissions we have set for the other user if any exist again as a final failsafe
            var existingDbPerms = await DbContext.UserGlobalPermissions
                .SingleOrDefaultAsync(p => p.UserUID == user.UID).ConfigureAwait(false);
            // if this is null as well, then we just need to inject the new permissions into the database.
            if (existingDbPerms == null)
            {
                await DbContext.UserGlobalPermissions.AddAsync(globalPerms).ConfigureAwait(false);
            }
            // otherwise, we need to update the existing permissions with the new permissions to refresh them.
            else
            {
                // update the client permissions with the new permissions, but do not change the user.
                globalPerms.Safeword = "NONE SET";
                globalPerms.SafewordUsed = false;
                globalPerms.CommandsFromFriends = false;
                globalPerms.CommandsFromParty = false;
                globalPerms.LiveChatGarblerActive = false;
                globalPerms.LiveChatGarblerLocked = false;
                globalPerms.WardrobeEnabled = false;
                globalPerms.ItemAutoEquip = false;
                globalPerms.RestraintSetAutoEquip = false;
                globalPerms.LockGagStorageOnGagLock = false;
                globalPerms.PuppeteerEnabled = false;
                globalPerms.GlobalTriggerPhrase = "";
                globalPerms.GlobalAllowSitRequests = false;
                globalPerms.GlobalAllowMotionRequests = false;
                globalPerms.GlobalAllowAllRequests = false;
                globalPerms.MoodlesEnabled = false;
                globalPerms.ToyboxEnabled = false;
                globalPerms.LockToyboxUI = false;
                globalPerms.ToyIsActive = false;
                globalPerms.ToyIntensity = 0;
                globalPerms.SpatialVibratorAudio = false;

                // update the existing permissions to the new refreshed permissions.
                DbContext.UserGlobalPermissions.Update(existingDbPerms);
            }
        }


        var permissions = existingData?.ownPairPermissions;
        // if the permissions don't exist, aka we dont have any permissions stored for this person yet.
        if (permissions == null)
        {
            // create our own permissions.  generate new permissions.
            permissions = new ClientPairPermissions() { User = user, OtherUser = otherUser };

            // grab the existing permissions we have set for the other user if any exist again as a final failsafe
            var existingDbPerms = await DbContext.ClientPairPermissions
                .SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == user.UID).ConfigureAwait(false);
            // if this is null as well, then we just need to inject the new permissions into the database.
            if (existingDbPerms == null)
            {
                await DbContext.ClientPairPermissions.AddAsync(permissions).ConfigureAwait(false);
            }
            // otherwise, we need to update the existing permissions with the new permissions to refresh them.
            else
            {
                // update the client permissions with the new permissions, but do not change the user.
                existingDbPerms.ExtendedLockTimes = false;
                existingDbPerms.MaxLockTime = TimeSpan.Zero;
                existingDbPerms.InHardcore = false;

                existingDbPerms.ApplyRestraintSets = false;
                existingDbPerms.LockRestraintSets = false;
                existingDbPerms.MaxAllowedRestraintTime = TimeSpan.Zero;
                existingDbPerms.RemoveRestraintSets = false;

                existingDbPerms.TriggerPhrase = "";
                existingDbPerms.StartChar = '(';
                existingDbPerms.EndChar = ')';
                existingDbPerms.AllowSitRequests = false;
                existingDbPerms.AllowMotionRequests = false;
                existingDbPerms.AllowAllRequests = false;

                existingDbPerms.AllowPositiveStatusTypes = false;
                existingDbPerms.AllowNegativeStatusTypes = false;
                existingDbPerms.AllowSpecialStatusTypes = false;
                existingDbPerms.PairCanApplyOwnMoodlesToYou = false;
                existingDbPerms.PairCanApplyYourMoodlesToYou = false;
                existingDbPerms.MaxMoodleTime = TimeSpan.Zero;
                existingDbPerms.AllowPermanentMoodles = false;

                existingDbPerms.ChangeToyState = false;
                existingDbPerms.CanControlIntensity = false;
                existingDbPerms.VibratorAlarms = false;
                existingDbPerms.CanUseRealtimeVibeRemote = false;
                existingDbPerms.CanExecutePatterns = false;
                existingDbPerms.CanExecuteTriggers = false;
                existingDbPerms.CanCreateTriggers = false;
                existingDbPerms.CanSendTriggers = false;

                existingDbPerms.AllowForcedFollow = false;
                existingDbPerms.IsForcedToFollow = false;
                existingDbPerms.AllowForcedSit = false;
                existingDbPerms.IsForcedToSit = false;
                existingDbPerms.AllowForcedToStay = false;
                existingDbPerms.IsForcedToStay = false;
                existingDbPerms.AllowBlindfold = false;
                existingDbPerms.ForceLockFirstPerson = false;
                existingDbPerms.IsBlindfolded = false;

                // update the existing permissions to the new refreshed permissions.
                DbContext.ClientPairPermissions.Update(existingDbPerms);
            }
        }

        // do the same for the permissions access, refer to comments above
        var permissionsAccess = existingData?.ownPairPermissionAccess;
        if (permissionsAccess == null)
        {
            permissionsAccess = new ClientPairPermissionAccess() { User = user, OtherUser = otherUser };

            var existingDbPermsAccess = await DbContext.ClientPairPermissionAccess
                .SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == user.UID).ConfigureAwait(false);
            if (existingDbPermsAccess == null)
            {
                await DbContext.ClientPairPermissionAccess.AddAsync(permissionsAccess).ConfigureAwait(false);
            }
            else
            {
                // unique permissions stored here:
                existingDbPermsAccess.CommandsFromFriendsAllowed = false; // Global
                existingDbPermsAccess.CommandsFromPartyAllowed = false; // Global
                existingDbPermsAccess.LiveChatGarblerActiveAllowed = false; // Global
                existingDbPermsAccess.LiveChatGarblerLockedAllowed = false; // Global
                existingDbPermsAccess.ExtendedLockTimesAllowed = false;
                existingDbPermsAccess.MaxLockTimeAllowed = false;
                // unique permissions for the wardrobe
                existingDbPermsAccess.WardrobeEnabledAllowed = false; // Global
                existingDbPermsAccess.ItemAutoEquipAllowed = false; // Global
                existingDbPermsAccess.RestraintSetAutoEquipAllowed = false; // Global
                existingDbPermsAccess.LockGagStorageOnGagLockAllowed = false; // Global
                existingDbPermsAccess.ApplyRestraintSetsAllowed = false;
                existingDbPermsAccess.LockRestraintSetsAllowed = false;
                existingDbPermsAccess.MaxAllowedRestraintTimeAllowed = false;
                existingDbPermsAccess.RemoveRestraintSetsAllowed = false;
                // unique permissions for the puppeteer
                existingDbPermsAccess.PuppeteerEnabledAllowed = false; // Global
                existingDbPermsAccess.AllowSitRequestsAllowed = false;
                existingDbPermsAccess.AllowMotionRequestsAllowed = false;
                existingDbPermsAccess.AllowAllRequestsAllowed = false;
                // unique Moodles permissions
                existingDbPermsAccess.MoodlesEnabledAllowed = false; // Global
                existingDbPermsAccess.AllowPositiveStatusTypesAllowed = false;
                existingDbPermsAccess.AllowNegativeStatusTypesAllowed = false;
                existingDbPermsAccess.AllowSpecialStatusTypesAllowed = false;
                existingDbPermsAccess.PairCanApplyOwnMoodlesToYouAllowed = false;
                existingDbPermsAccess.PairCanApplyYourMoodlesToYouAllowed = false;
                existingDbPermsAccess.MaxMoodleTimeAllowed = false;
                existingDbPermsAccess.AllowPermanentMoodlesAllowed = false;
                // unique permissions for the toybox
                existingDbPermsAccess.ToyboxEnabledAllowed = false; // Global
                existingDbPermsAccess.LockToyboxUIAllowed = false; // Global
                existingDbPermsAccess.ToyIsActiveAllowed = false; // Global
                existingDbPermsAccess.SpatialVibratorAudioAllowed = false; // Global
                existingDbPermsAccess.ChangeToyStateAllowed = false;
                existingDbPermsAccess.CanControlIntensityAllowed = false;
                existingDbPermsAccess.VibratorAlarmsAllowed = false;
                existingDbPermsAccess.CanUseRealtimeVibeRemoteAllowed = false;
                existingDbPermsAccess.CanExecutePatternsAllowed = false;
                existingDbPermsAccess.CanExecuteTriggersAllowed = false;
                existingDbPermsAccess.CanCreateTriggersAllowed = false;
                existingDbPermsAccess.CanSendTriggersAllowed = false;

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
        GagSpeak.API.Data.Permissions.UserGlobalPermissions ownGlobalPerms = globalPerms.ToApiGlobalPerms();
        GagSpeak.API.Data.Permissions.UserPairPermissions ownPairPerms = permissions.ToApiUserPairPerms();
        GagSpeak.API.Data.Permissions.UserEditAccessPermissions ownAccessPerms = permissionsAccess.ToApiUserPairEditAccessPerms();
        GagSpeak.API.Data.Permissions.UserGlobalPermissions otherGlobalPerms = otherGlobalPermissions.ToApiGlobalPerms();
        GagSpeak.API.Data.Permissions.UserPairPermissions otherPerms = otherPermissions.ToApiUserPairPerms();
        GagSpeak.API.Data.Permissions.UserEditAccessPermissions otherPermsAccess = otherPermissionsAccess.ToApiUserPairEditAccessPerms();

        // construct a new UserPairDto based on the response
        UserPairDto userPairResponse = new UserPairDto(
            otherUser.ToUserData(),
            otherEntry == null ? IndividualPairStatus.OneSided : IndividualPairStatus.Bidirectional,
            ownGlobalPerms,
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

        // fetch all the pair information of the client caller
        var pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        // return the list of UserPair DTO's containing the paired clients of the client caller
        return pairs.Select(p =>
        {
            return new UserPairDto(new UserData(p.Key, p.Value.Alias),
                p.Value.ToIndividualPairStatus(),
                p.Value.ownGlobalPerms.ToApiGlobalPerms(),
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
        if (!pairData.IndividuallyPaired) return;

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