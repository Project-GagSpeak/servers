using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Enums;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Data;

namespace GagspeakServer.Hubs;

/// <summary> 
/// This partial class of the GagSpeakHub contains all the user related methods 
/// </summary>
public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task UserSendPairRequest(UserPairSendRequestDto dto)
    {
        // log the call info.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure that the user we want to send a request to is not ourselves.
        var uid = dto.User.UID.Trim();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot send a Kinkster Request to self").ConfigureAwait(false);
            return;
        }

        // return invalid if the user we wanna add is not in the database.
        var otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        if (otherUser == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot send Kinkster Request to {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return;
        }

        // if a client pair relation between you and the other user already exists in client pairs. return invalid.
        var existingPair = await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == otherUser.UID) || (p.UserUID == otherUser.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingPair)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot send Kinkster Request to {dto.User.UID}, already paired").ConfigureAwait(false);
            return;
        }


        // verify an existing entry does not already exist.
        var existingRequest = await DbContext.KinksterPairRequests.AnyAsync(k => (k.UserUID == UserUID && k.OtherUserUID == otherUser.UID) || (k.UserUID == otherUser.UID && k.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingRequest)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"A Request for this Kinkster is already present").ConfigureAwait(false);
            return;
        }

        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // create a new KinksterPairRequest object, and add it to the database.
        KinksterRequest request = new KinksterRequest()
        {
            User = user,
            OtherUser = otherUser,
            CreationTime = DateTime.UtcNow,
            AttachedMessage = dto.AttachedMessage
        };
        // append the request to the DB.
        await DbContext.KinksterPairRequests.AddAsync(request).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);


        // send back to both pairs that they have a new kinkster request.
        var newDto = new UserPairRequestDto(new(user.UID), new(otherUser.UID), dto.AttachedMessage, request.CreationTime);
        await Clients.User(UserUID).Client_UserAddPairRequest(newDto).ConfigureAwait(false);
        // send the request to them if the other user is online as well.
        var otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);
        if (otherIdent is null) return;
        // if they are, send the request to them.
        await Clients.User(otherUser.UID).Client_UserAddPairRequest(newDto).ConfigureAwait(false);
    }

    [Authorize(Policy = "Identified")]
    public async Task UserCancelPairRequest(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure that the user we want to cancel a request to is not ourselves.
        var uid = dto.User.UID.Trim();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot cancel a Kinkster Request to self").ConfigureAwait(false);
            return;
        }

        // return invalid if the user we wanna add is not in the database.
        var otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        if (otherUser is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot cancel Kinkster Request to {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return;
        }

        // if the existing entry was removed or no longer exists, notify them it was expired.
        var existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == otherUser.UID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot cancel Kinkster Request to {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return;
        }

        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // notify the client caller and the recipient that the request was cancelled.
        var newDto = new UserPairRequestDto(new(user.UID), new(otherUser.UID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Client_UserRemovePairRequest(newDto).ConfigureAwait(false);

        // send off to the other user.
        var otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);
        if (otherIdent is not null) await Clients.User(otherUser.UID).Client_UserRemovePairRequest(newDto).ConfigureAwait(false);

        // now we can safely remove it from the DB and save changes.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// PLEASE KEEP IN MIND THAT THE PERSON ACCEPTING THIS IS NOT THE PERSON WHO MADE THE INVITE, 
    /// YOU MUST INVERT THE ORDER TO MATCH.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserAcceptIncPairRequest(UserDto dto)
    {
        // verify that we did not try to accept ourselves.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID)) return;

        // Verify the person who sent this request still has an account.
        var pairRequesterUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == dto.User.UID || u.Alias == dto.User.UID).ConfigureAwait(false);
        if (pairRequesterUser is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot accept Kinkster Request from {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return;
        }

        // verify that the request exists in the database. (the pairRequesterUser would be the UserUID, we are OtherUserUID)
        var existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == pairRequesterUser.UID && k.OtherUserUID == UserUID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot accept Kinkster Request from {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return;
        }

        // ensure that the client pair entry is not already existing.
        var existingEntry = await DbContext.ClientPairs.AsNoTracking().FirstOrDefaultAsync(p => p.User.UID == UserUID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
        if (existingEntry is not null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, already paired").ConfigureAwait(false);
            return;
        }

        // create a client pair entry for the client caller and the other user.
        var pairRequestAcceptingUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        ClientPair callerToRecipient = new ClientPair() { User = pairRequestAcceptingUser, OtherUser = pairRequesterUser, };
        ClientPair recipientToCaller = new ClientPair() { User = pairRequesterUser, OtherUser = pairRequestAcceptingUser, };
        // add this clientpair relation to the database
        await DbContext.ClientPairs.AddAsync(callerToRecipient).ConfigureAwait(false);
        await DbContext.ClientPairs.AddAsync(recipientToCaller).ConfigureAwait(false);

        // Obtain ALL relevant information about the relationship between these pairs.
        // This includes their current global perms, pair perms, and pair perms access.
        // If none are present, creates new versions.
        var existingData = await GetPairInfo(UserUID, pairRequesterUser.UID).ConfigureAwait(false);


        // store the existing data permission items to objects for setting if null.
        var ownGlobals = existingData?.ownGlobalPerms;
        var ownPairPerms = existingData?.ownPairPermissions;
        var ownPairPermsAccess = existingData?.ownPairPermissionAccess;
        var otherGlobals = existingData?.otherGlobalPerms;
        var otherPairPerms = existingData?.otherPairPermissions;
        var otherPairPermsAccess = existingData?.otherPairPermissionAccess;
        // Handle OwnGlobals
        if (ownGlobals is null)
        {
            var existingOwnGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if(existingOwnGlobals is null)
            {
                ownGlobals = new UserGlobalPermissions() { User = pairRequestAcceptingUser };
                await DbContext.UserGlobalPermissions.AddAsync(ownGlobals).ConfigureAwait(false);
            }
            else
            {
                DbContext.UserGlobalPermissions.Update(existingOwnGlobals);
            }
        }
        // Handle OwnPerms
        if (ownPairPerms is null)
        {
            var existingOwnPairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if(existingOwnPairPerms is null)
            {
                ownPairPerms = new ClientPairPermissions() { User = pairRequestAcceptingUser, OtherUser = pairRequesterUser };
                await DbContext.ClientPairPermissions.AddAsync(ownPairPerms).ConfigureAwait(false);
            }
            else
            {
                DbContext.ClientPairPermissions.Update(existingOwnPairPerms);
            }
        }

        if (ownPairPermsAccess is null)
        {
            var existingOwnPairPermsAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if(existingOwnPairPermsAccess is null)
            {
                ownPairPermsAccess = new ClientPairPermissionAccess() { User = pairRequestAcceptingUser, OtherUser = pairRequesterUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(ownPairPermsAccess).ConfigureAwait(false);
            }
            else
            {
                DbContext.ClientPairPermissionAccess.Update(existingOwnPairPermsAccess);
            }
        }

        if (otherGlobals is null)
        {
            var existingOtherGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if(existingOtherGlobals is null)
            {
                otherGlobals = new UserGlobalPermissions() { User = pairRequesterUser };
                await DbContext.UserGlobalPermissions.AddAsync(otherGlobals).ConfigureAwait(false);
            }
            else
            {
                DbContext.UserGlobalPermissions.Update(existingOtherGlobals);
            }
        }

        if (otherPairPerms is null)
        {
            var existingOtherPairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID && p.OtherUserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if(existingOtherPairPerms is null)
            {
                otherPairPerms = new ClientPairPermissions() { User = pairRequesterUser, OtherUser = pairRequestAcceptingUser };
                await DbContext.ClientPairPermissions.AddAsync(otherPairPerms).ConfigureAwait(false);
            }
            else
            {
                DbContext.ClientPairPermissions.Update(existingOtherPairPerms);
            }
        }

        if (otherPairPermsAccess is null)
        {
            var existingOtherPairPermsAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID && p.OtherUserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if (existingOtherPairPermsAccess is null)
            {
                otherPairPermsAccess = new ClientPairPermissionAccess() { User = pairRequesterUser, OtherUser = pairRequestAcceptingUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(otherPairPermsAccess).ConfigureAwait(false);
            }
            else
            {
                DbContext.ClientPairPermissionAccess.Update(existingOtherPairPermsAccess);
            }
        }

        // remove the actual request from the database.
        DbContext.KinksterPairRequests.Remove(existingRequest);

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // compile the api data objects.
        var ownGlobalsApi = ownGlobals.ToApiGlobalPerms();
        var ownPairPermsApi = ownPairPerms.ToApiUserPairPerms();
        var ownPairPermsAccessApi = ownPairPermsAccess.ToApiUserPairEditAccessPerms();
        var otherGlobalsApi = otherGlobals.ToApiGlobalPerms();
        var otherPairPermsApi = otherPairPerms.ToApiUserPairPerms();
        var otherPairPermsAccessApi = otherPairPermsAccess.ToApiUserPairEditAccessPerms();

        // construct a new UserPairDto based on the response
        UserPairDto pairRequestAcceptingUserResponse = new UserPairDto(pairRequesterUser.ToUserData(), IndividualPairStatus.Bidirectional,
            ownPairPermsApi, ownPairPermsAccessApi, otherGlobalsApi, otherPairPermsApi, otherPairPermsAccessApi);

        // inform the client caller's user that the pair was added successfully, to add the pair to their pair manager.
        var removeRequestDto = new UserPairRequestDto(new(existingRequest.UserUID), new(existingRequest.OtherUserUID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Client_UserRemovePairRequest(removeRequestDto).ConfigureAwait(false);
        await Clients.User(pairRequestAcceptingUser.UID).Client_UserAddClientPair(pairRequestAcceptingUserResponse).ConfigureAwait(false);

        // check if other user is online
        var otherIdent = await GetUserIdent(pairRequesterUser.UID).ConfigureAwait(false);
        // do not send update to other user if they are not online.
        if (otherIdent is null)
            return;

        UserPairDto pairRequesterUserResponse = new UserPairDto(pairRequestAcceptingUser.ToUserData(), IndividualPairStatus.Bidirectional,
            otherPairPermsApi, otherPairPermsAccessApi, ownGlobalsApi, ownPairPermsApi, ownPairPermsAccessApi);

        // They are online, so let them know to add the client pair to their pair manager.
        await Clients.User(pairRequesterUser.UID).Client_UserRemovePairRequest(removeRequestDto).ConfigureAwait(false);
        await Clients.User(pairRequesterUser.UID).Client_UserAddClientPair(pairRequesterUserResponse).ConfigureAwait(false);

        await Clients.User(UserUID).Client_UserSendOnline(new(pairRequesterUser.ToUserData(), otherIdent)).ConfigureAwait(false);
        await Clients.User(pairRequesterUser.UID).Client_UserSendOnline(new(pairRequestAcceptingUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);

        // Initialize a group for the two pairs that will be automatically disposed of 5 minutes after its creation.
        var sortedUIDs = new[] { pairRequesterUser.UID, pairRequestAcceptingUser.UID }.OrderBy(uid => uid, StringComparer.Ordinal).ToArray();
        var groupName = $"PairChat-{sortedUIDs[0]}-{sortedUIDs[1]}";

        if (_userConnections.TryGetValue(pairRequesterUser.UID, out var connectionIdA) && _userConnections.TryGetValue(pairRequestAcceptingUser.UID, out var connectionIdB))
        {
            await Groups.AddToGroupAsync(connectionIdA, groupName).ConfigureAwait(false);
            await Groups.AddToGroupAsync(connectionIdB, groupName).ConfigureAwait(false);

            // use the message requester to send the initial message.
            await Clients.Group(groupName).Client_PairChatMessage(new(new("SYSTEM-MSG"), groupName, "This Chat will exist for 10 minutes and then close " +
                "automatically! Take advantage of it to establish another way to contact eachother or meetup, locations ext. Have fun!")).ConfigureAwait(false);

            // Store the group information in the internal storage with an expiration time to 5 minutes from the current time.
            var expiresAt = DateTime.UtcNow.AddMinutes(10);
            _activeGroups[groupName] = (connectionIdA, connectionIdB, expiresAt);
        }
    }

    private async Task RemoveGroup(string groupName, string connectionIdA, string connectionIdB)
    {
        // Remove both users from the group
        if (!string.IsNullOrEmpty(connectionIdA))
        {
            await Groups.RemoveFromGroupAsync(connectionIdA, groupName).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(connectionIdB))
        {
            await Groups.RemoveFromGroupAsync(connectionIdB, groupName).ConfigureAwait(false);
        }
    }
    public async Task SendPairChat(PairChatMessageDto message)
    {
        await Clients.Group(message.GroupName).Client_PairChatMessage(message).ConfigureAwait(false);
    }

    [Authorize(Policy = "Identified")]
    public async Task UserRejectIncPairRequest(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // grab the existing request from the database.
        var existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == dto.User.UID && k.OtherUserUID == UserUID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot reject Kinkster Request from {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return;
        }

        // send to both users to remove the kinkster request.
        var rejectionDto = new UserPairRequestDto(new(existingRequest.UserUID), new(existingRequest.OtherUserUID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Client_UserRemovePairRequest(rejectionDto).ConfigureAwait(false);

        // send it to the other person if they are online at the time as well.
        var otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent is not null)
        {
            await Clients.User(dto.User.UID).Client_UserRemovePairRequest(rejectionDto).ConfigureAwait(false);
        }

        // remove the request from the database.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
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
        if (callerPair is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot remove {dto.User.UID} from your client pair list, the pair does not exist").ConfigureAwait(false);
            return;
        }

        // Get pair info of the user we are removing
        var pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

        // remove the client pair from the database and all associated permissions. And then update changes
        DbContext.ClientPairs.Remove(callerPair);
        if (pairData?.ownPairPermissions is not null) DbContext.ClientPairPermissions.Remove(pairData.ownPairPermissions);
        if (pairData?.ownPairPermissionAccess is not null) DbContext.ClientPairPermissionAccess.Remove(pairData.ownPairPermissionAccess);
        // remove the other user's permissions as well.
        // grab the clientPairs item for the other direction.
        var otherPair = await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == dto.User.UID && w.OtherUserUID == UserUID).ConfigureAwait(false);
        if (otherPair is not null)
        {
            DbContext.ClientPairs.Remove(otherPair);
            if (pairData?.otherPairPermissions is not null) DbContext.ClientPairPermissions.Remove(pairData.otherPairPermissions);
            if (pairData?.otherPairPermissionAccess is not null) DbContext.ClientPairPermissionAccess.Remove(pairData.otherPairPermissionAccess);
        }
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));
        // return to the client callers callback functions that we should remove them from the client callers pair manager.
        await Clients.User(UserUID).Client_UserRemoveClientPair(dto).ConfigureAwait(false);

        // Check if the other user is online.
        var otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent is null)
            return;

        // if they are, we should ask them to remove the client pair from thier listing as well.
        await Clients.User(dto.User.UID).Client_UserRemoveClientPair(new(new(UserUID))).ConfigureAwait(false);
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
            if (user is not null) await DeleteUser(user).ConfigureAwait(false);
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
        //_logger.LogCallInfo();

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
            if (ownPairPermissionAccess == null)
            {
                _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {UserUID} in relation towards {otherUser.UID}, creating new one.");
                ownPairPermissionAccess = new ClientPairPermissionAccess() { User = ClientCallerUser, OtherUser = otherUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(ownPairPermissionAccess).ConfigureAwait(false);
            }

            // fetch the other users global permissions
            var otherGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Global Permissions & add it to the database.
            if (otherGlobalPerms == null)
            {
                _logger.LogMessage($"No GlobalPermissions TableRow was found for User: {otherUser.UID}, creating new one.");
                otherGlobalPerms = new UserGlobalPermissions() { User = otherUser };
                await DbContext.UserGlobalPermissions.AddAsync(otherGlobalPerms).ConfigureAwait(false);
            }

            // fetch the other users pair permissions
            var otherPairPermissions = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Pair Permissions & add it to the database.
            if (otherPairPermissions == null)
            {
                _logger.LogMessage($"No PairPermissions TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
                otherPairPermissions = new ClientPairPermissions() { User = otherUser, OtherUser = ClientCallerUser };
                await DbContext.ClientPairPermissions.AddAsync(otherPairPermissions).ConfigureAwait(false);
            }

            // fetch the other users pair permissions access
            var otherPairPermissionAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == otherUser.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
            // check if null, if so, create new OtherUser Pair Permissions Access & add it to the database.
            if (otherPairPermissionAccess == null)
            {
                _logger.LogMessage($"No PairPermissionsAccess TableRow was found for User: {otherUser.UID} in relation towards {UserUID}, creating new one.");
                otherPairPermissionAccess = new ClientPairPermissionAccess() { User = otherUser, OtherUser = ClientCallerUser };
                await DbContext.ClientPairPermissionAccess.AddAsync(otherPairPermissionAccess).ConfigureAwait(false);
            }
        }
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch all the pair information of the client caller
        var pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        // return the list of UserPair DTO's containing the paired clients of the client caller
        return pairs.Select(p =>
        {
            var pairList = new UserPairDto(new UserData(p.Key, p.Value.Alias, p.Value.SupporterTier, p.Value.createdDate),
                p.Value.ToIndividualPairStatus(),
                p.Value.ownPairPermissions.ToApiUserPairPerms(),
                p.Value.ownPairPermissionAccess.ToApiUserPairEditAccessPerms(),
                p.Value.otherGlobalPerms.ToApiGlobalPerms(),
                p.Value.otherPairPermissions.ToApiUserPairPerms(),
                p.Value.otherPairPermissionAccess.ToApiUserPairEditAccessPerms());
            return pairList;
        }).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<List<UserPairRequestDto>> UserGetPairRequests()
    {
        // fetch all the pair requests with the UserUid in either the UserUID or OtherUserUID
        var requests = await DbContext.KinksterPairRequests.Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID).ToListAsync().ConfigureAwait(false);

        // return the list of UserPairRequest DTO's containing the pair requests of the client caller
        return requests.Select(r => new UserPairRequestDto(new(r.UserUID), new(r.OtherUserUID), r.AttachedMessage, r.CreationTime)).ToList();
    }


    /// <summary>
    /// Sends an action to a paired users Shock Collar.
    /// Must verify they are in hardcore mode to proceed.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserShockActionOnPair(ShockCollarActionDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot send a shock request to yourself! (yet?)").ConfigureAwait(false);
            return;
        }

        // make sure they are added as a pair of the client caller.
        var userPairGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        var userPairPermsForCaller = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (userPairPermsForCaller == null || userPairGlobalPerms == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "User is not paired with you").ConfigureAwait(false);
            return;
        }

        // ensure that this user is in hardcore mode with you.
        if (!userPairPermsForCaller.InHardcore ||
        (userPairGlobalPerms.GlobalShockShareCode.IsNullOrEmpty() && userPairPermsForCaller.ShockCollarShareCode.IsNullOrEmpty()))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "User is not in hardcore mode with you, or " +
                "doesn't have any shock collars configured!").ConfigureAwait(false);
            return;
        }

        // otherwise, it is valid, so attempt to send the shock instruction to the user.
        await Clients.User(dto.User.UID).Client_UserReceiveShockInstruction(new(UserUID.ToUserDataFromUID(), dto.OpCode, dto.Intensity, dto.Duration)).ConfigureAwait(false);
    }


    /// <summary>
    /// Called by a connected client who wishes to retrieve the profile of another user.
    /// </summary>
    /// <returns> The UserProfileDto of the user requested </returns>
    [Authorize(Policy = "Identified")]
    public async Task<UserKinkPlateDto> UserGetKinkPlate(UserDto user)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(user));

        // Grab all users Client Caller is paired with.
        var allUserPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        // Grab the requested user's profile data from the database
        UserProfileData? data = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.User.UID).ConfigureAwait(false);
        if (data == null)
        {
            var newPlate = new KinkPlateContent();
            return new UserKinkPlateDto(user.User, newPlate, string.Empty);
        }
        // If requested User Profile is not in list of pairs, and is not self, return blank profile update.
        if (!allUserPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            // if the profile is not public, and it was required from a non-paired user, return a blank profile.
            if (!data.ProfileIsPublic)
            {
                var newPlate = new KinkPlateContent() { Description = "Profile is not Public!" };
                return new UserKinkPlateDto(user.User, newPlate, string.Empty);
            }
        }
        if (data.ProfileDisabled)
        {
            var newPlate = new KinkPlateContent() { Disabled = true, Description = "This profile is currently disabled" };
            return new UserKinkPlateDto(user.User, newPlate, string.Empty);
        }
        // Return the valid profile.
        return new UserKinkPlateDto(user.User, data.FromProfileData(), data.Base64ProfilePic);
    }

    /// <summary>
    /// Called by the client who wishes to update the database with their latest achievement data.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserUpdateAchievementData(UserAchievementsDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto.User));

        // return if the client caller doesnt match the user dto.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot modify achievement data for anyone but yourself").ConfigureAwait(false);
            return;
        }

        // handle case where it was called after a user delete.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot Save Achievement Data for a Deleted User").ConfigureAwait(false);
            return;
        }

        // Grab Client Callers current profile data from the database
        var existingData = await DbContext.UserAchievementData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (existingData is not null)
        {
            if (!dto.AchievementDataBase64.IsNullOrEmpty())
            {
                existingData.Base64AchievementData = dto.AchievementDataBase64;
                // should also sync this with profile data down the line but for now dont worry about it.
            }
            else
            {
                // they tried to update with null data, which shouldnt ever happen.
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot update achievement data with null data").ConfigureAwait(false);
                return;
            }
        }
        // otherwise profile data has not been made, so create a fresh instance.
        else
        {
            UserAchievementData userProfileData = new()
            {
                UserUID = dto.User.UID,
                Base64AchievementData = dto.AchievementDataBase64,
            };
            await DbContext.UserAchievementData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // Save DB Changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary> 
    /// Called by a connected client who wishes to set or update their profile data.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserSetKinkPlate(UserKinkPlateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto.User));

        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot modify profile data for anyone but yourself").ConfigureAwait(false);
            return;
        }

        // Grab Client Callers current profile data from the database
        var existingData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);

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
            if (image.Width > 256 || image.Height > 256)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is larger than 256x256").ConfigureAwait(false);
                return;
            }
        }

        // Validate the rest of the profile data.
        if (existingData is not null)
        {
            // If this causes any errors then return to the possible null value it had.
            existingData.Base64ProfilePic = dto.ProfilePictureBase64;
            // update all other values from the Info in the dto.
            existingData.UpdateInfoFromDto(dto.Info);
        }
        else // If no data exists, our profile is not yet in the database, so create a fresh one and add it.
        {
            UserProfileData userProfileData = DataUpdateHelpers.NewPlateFromDto(dto);
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
    public async Task UserReportKinkPlate(UserKinkPlateReportDto dto)
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

        // Reporting if valid, so construct new report object, at the current time as a snapshot, to avoid tempering by the reported user post-report.
        UserProfileDataReport reportToAdd = new()
        {
            ReportTime = DateTime.UtcNow,
            ReportedBase64Picture = profile.Base64ProfilePic,
            ReportedDescription = profile.UserDescription,
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

        // DO NOT INFORM CLIENT THEIR PROFILE HAS BEEN REPORTED. THIS IS TO MAINTAIN CONFIDENTIALITY OF REPORTS.
        // if we did, people who got reported would go on a witch hunt for the people they have added. This is not ok to have.
        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information, "Your Report has been made successfully, and is now pending validation from CK").ConfigureAwait(false);

        // Notify other user pairs to update their profiles, so they obtain the latest information, including the profile, now being flagged.
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);
        await Clients.Users(pairs.Select(p => p.Key)).Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
        await Clients.Caller.Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
    }
}