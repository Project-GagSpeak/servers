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
using GagspeakAPI.Dto.Toybox;

namespace GagspeakServer.Hubs;

/// <summary> 
/// This partial class of the GagSpeakHub contains all the user related methods 
/// </summary>
public partial class GagspeakHub
{

    /// <summary> 
    /// Called by a connected client who wishes to add another User as a pair.
    /// <para>
    /// Creates a new initial client pair object for 2 users within the database 
    /// and returns the successful object to the clients.
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
        await Clients.User(otherUser.UID).Client_UserUpdateOtherAllPairPerms(
            new UserPairUpdateAllPermsDto(user.ToUserData(), ownGlobalPerms, ownPairPerms, ownAccessPerms, false)).ConfigureAwait(false);

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
    /// Called by a connected client who wishes to remove a user from their paired list.
    /// </summary>>
    [Authorize(Policy = "Identified")]
    public async Task UserRemovePair(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Dont allow removing self
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) return;

        // See if clientPair exists at all in the database
        var callerPair = await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (callerPair == null) throw new Exception("ClientPair was null, this should not happen.");

        // Get pair info of the user we are removing
        var pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);
        if (pairData == null) throw new Exception("PairData was null, this should not happen.");

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
    /// Called by a connected client who wishes to delete their account from the server.
    /// <para> 
    /// Method will remove all associated things with the user and delete their profile from 
    /// the server, along with all other profiles under their account.
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
    /// Requested by the client caller upon login, asking to get all current client pairs of them that are online.
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
    /// Called by a connected client who wishes to retrieve the list of paired clients via a list of UserPairDto's.
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


        foreach(var pair in pairs)
        {
            _logger.LogWarning($"Other Pair Access ApplyRestraintSetsAllowed {pair.Key}: {pair.Value.otherPairPermissionAccess.ApplyRestraintSetsAllowed}");
        }


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
    /// Called by a connected client who wishes to retrieve the profile of another user.
    /// </summary>
    /// <returns> The UserProfileDto of the user requested </returns>
    [Authorize(Policy = "Identified")]
    public async Task<UserProfileDto> UserGetProfile(UserDto user)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(user));

        // Grab all users Client Caller is paired with.
        var allUserPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        // If requested User Profile is not in list of pairs, and is not self, return blank profile update.
        if (!allUserPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            return new UserProfileDto(user.User, false, null, "Due to the pause status you cannot access this users profile.");
        }

        // Grab the requested user's profile data from the database
        UserProfileData? data = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.User.UID).ConfigureAwait(false);
        if (data == null) return new UserProfileDto(user.User, false, null, null); // return a null profile if invalid.
        if (data.ProfileDisabled) return new UserProfileDto(user.User, true, null, "This profile is currently disabled");

        // Return the valid profile. (nothing necessary to push to other pairs).
        return new UserProfileDto(user.User, false, data.Base64ProfilePic, data.UserDescription);
    }

    /// <summary> 
    /// Called by a connected client who wishes to set or update their profile data.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserSetProfile(UserProfileDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new HubException("Cannot modify profile data for anyone but yourself");

        // Grab Client Callers current profile data from the database
        var existingData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (existingData?.ProfileDisabled ?? false) 
        {
            // possibly not do this, but rather just ban the user outright if they are disabled? Idk
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your profile was disabled and cannot be edited").ConfigureAwait(false);
            return;
        }

        // Grab the new ProfilePictureData if it exists
        if (!string.IsNullOrEmpty(dto.ProfilePictureBase64))
        {
            // Convert from the base64 into raw image data bytes.
            byte[] imageData = Convert.FromBase64String(dto.ProfilePictureBase64);
            
            // Load the image into a memory stream
            using MemoryStream ms = new(imageData);
            
            // Detect format of the image
            var format = await Image.DetectFormatAsync(ms).ConfigureAwait(false);
            
            // Ensure it is a png format, throw exception if it is not.
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is not in PNG format").ConfigureAwait(false);
                return;
            }

            // Temporarily load the image into memory from the image data to check its ImageSize & FileSize.
            using var image = Image.Load<Rgba32>(imageData);

            // Ensure Image meets required parameters.
            if (image.Width > 256 || image.Height > 256 || (imageData.Length > 250 * 1024))
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is larger than 256x256 or more than 250KiB.").ConfigureAwait(false);
                return;
            }
        }

        // Validate the rest of the profile data.
        if (existingData != null)
        {
            // Set ProfilePictureBase64 to null if string is not provided.
            if (string.Equals("", dto.ProfilePictureBase64, StringComparison.OrdinalIgnoreCase))
            {
                existingData.Base64ProfilePic = null;
            }
            // If string is provided, set the new Base64ProfilePic.
            else if (dto.ProfilePictureBase64 != null)
            {
                existingData.Base64ProfilePic = dto.ProfilePictureBase64;
            }
            // If description contains content, updated the description.
            if (dto.Description != null)
            {
                existingData.UserDescription = dto.Description;
            }
        }
        else // If no data exists, our profile is not yet in the database, so create a fresh one and add it.
        {
            UserProfileData userProfileData = new()
            {
                UserUID = dto.User.UID,
                Base64ProfilePic = dto.ProfilePictureBase64 ?? null,
                UserDescription = dto.Description ?? null,
            };
            await DbContext.UserProfileData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // Save DB Changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Fetch all paired user's of the client caller
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // Get Online users.
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // Inform the client caller and all their pairs that their profile has been updated.
        await Clients.Users(pairs.Select(p => p.Key)).Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
        await Clients.Caller.Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
    }


    [Authorize(Policy = "Identified")]
    public async Task UserReportProfile(UserProfileReportDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // if the client caller has already reported this profile, inform them and return.
        UserProfileDataReport? report = await DbContext.UserProfileReports.SingleOrDefaultAsync(u => u.ReportedUserUID == dto.User.UID && u.ReportingUserUID == UserUID).ConfigureAwait(false);
        if (report != null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "You already reported this profile and it's pending validation").ConfigureAwait(false);
            return;
        }

        // grab the profile of the user being reported. If it doesn't exist, inform the client caller and return.
        UserProfileData? profile = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (profile == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "This user has no profile").ConfigureAwait(false);
            return;
        }

        // Reporting if valid, so construct new report object.
        UserProfileDataReport reportToAdd = new()
        {
            ReportTime = DateTime.UtcNow,
            ReportingUserUID = UserUID,
            ReportReason = dto.ProfileReport,
            ReportedUserUID = dto.User.UID,
        };

        // mark the profile as flagged for report (possibly remove this as i dont see much purpose)
        profile.FlaggedForReport = true;

        // add it to the table of reports in the database & save changes.
        await DbContext.UserProfileReports.AddAsync(reportToAdd).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        // log the report
        _logger.LogWarning($"User {UserUID} reported user {dto.User.UID} for {dto.ProfileReport}");
    }


        /// <summary> A small helper function to get the opposite entry of a client pair (how its viewed from the other side) </summary>
    private ClientPair OppositeEntry(string otherUID) =>
        DbContext.ClientPairs.AsNoTracking().SingleOrDefault(w => w.User.UID == otherUID && w.OtherUser.UID == UserUID);
}