using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendKinksterRequest(CreateKinksterRequest dto)
    {
        // log the call info.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure that the user we want to send a request to is not ourselves.
        string uid = dto.User.UID.Trim();

        // return invalid if the user we wanna add is not in the database.
        User? otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        if (otherUser is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot send Kinkster Request to {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // if this "otherUser" is ourselves, return invalid.
        if (string.Equals(otherUser.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot send Kinkster Request to self").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // if a client pair relation between you and the other user already exists in client pairs. return invalid.
        bool existingPair = await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == otherUser.UID) || (p.UserUID == otherUser.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingPair)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot send Kinkster Request to {dto.User.UID}, already paired").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPaired);
        }


        // verify an existing entry does not already exist.
        bool existingRequest = await DbContext.KinksterPairRequests.AnyAsync(k => (k.UserUID == UserUID && k.OtherUserUID == otherUser.UID) || (k.UserUID == otherUser.UID && k.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingRequest)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"A Request for this Kinkster is already present").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestExists);
        }

        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // create a new KinksterPairRequest object, and add it to the database.
        KinksterRequest newRequest = new KinksterRequest()
        {
            User = user,
            OtherUser = otherUser,
            AttachedMessage = dto.Message,
            CreationTime = DateTime.UtcNow,
        };
        // append the request to the DB.
        await DbContext.KinksterPairRequests.AddAsync(newRequest).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send back to both pairs that they have a new kinkster request.
        KinksterRequestEntry newDto = new(user.ToUserData(), otherUser.ToUserData(), dto.Message, newRequest.CreationTime);
        await Clients.User(UserUID).Callback_AddPairRequest(newDto).ConfigureAwait(false);
        
        // send the request to them if the other user is online as well.
        string? otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);
        if (otherIdent is null)
            return HubResponseBuilder.Yippee();

        // if they are, send the request to them.
        await Clients.User(otherUser.UID).Callback_AddPairRequest(newDto).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserCancelKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure that the user we want to cancel a request to is not ourselves.
        string uid = dto.User.UID.Trim();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot cancel a Kinkster Request to self").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // return invalid if the user we wanna add is not in the database.
        User? otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        if (otherUser is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot cancel Kinkster Request to {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // if the existing entry was removed or no longer exists, notify them it was expired.
        KinksterRequest? existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == otherUser.UID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot cancel Kinkster Request to {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);
        }

        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // notify the client caller and the recipient that the request was cancelled.
        KinksterRequestEntry newDto = new(user.ToUserData(), otherUser.ToUserData(), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Callback_RemovePairRequest(newDto).ConfigureAwait(false);

        // send off to the other user.
        string? otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);
        if (otherIdent is not null) 
            await Clients.User(otherUser.UID).Callback_RemovePairRequest(newDto).ConfigureAwait(false);

        // now we can safely remove it from the DB and save changes.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    ///     PLEASE KEEP IN MIND THAT THE PERSON ACCEPTING THIS IS NOT THE PERSON WHO MADE THE INVITE, 
    ///     YOU MUST INVERT THE ORDER TO MATCH.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserAcceptKinksterRequest(KinksterBase dto)
    {
        // verify that we did not try to accept ourselves.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Verify the person who sent this request still has an account.
        User? pairRequesterUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == dto.User.UID || u.Alias == dto.User.UID).ConfigureAwait(false);
        if (pairRequesterUser is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot accept Kinkster Request from {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // verify that the request exists in the database. (the pairRequesterUser would be the UserUID, we are OtherUserUID)
        KinksterRequest? existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == pairRequesterUser.UID && k.OtherUserUID == UserUID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot accept Kinkster Request from {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);
        }

        // ensure that the client pair entry is not already existing.
        ClientPair? existingEntry = await DbContext.ClientPairs.AsNoTracking().FirstOrDefaultAsync(p => p.User.UID == UserUID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
        if (existingEntry is not null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, already paired").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPaired);
        }

        // create a client pair entry for the client caller and the other user.
        User pairRequestAcceptingUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        ClientPair callerToRecipient = new ClientPair() { User = pairRequestAcceptingUser, OtherUser = pairRequesterUser, };
        ClientPair recipientToCaller = new ClientPair() { User = pairRequesterUser, OtherUser = pairRequestAcceptingUser, };
        // add this clientpair relation to the database
        await DbContext.ClientPairs.AddAsync(callerToRecipient).ConfigureAwait(false);
        await DbContext.ClientPairs.AddAsync(recipientToCaller).ConfigureAwait(false);

        // Obtain ALL relevant information about the relationship between these pairs.
        // This includes their current global perms, pair perms, and pair perms access.
        // If none are present, creates new versions.
        UserInfo? existingData = await GetPairInfo(UserUID, pairRequesterUser.UID).ConfigureAwait(false);


        // store the existing data permission items to objects for setting if null.
        UserGlobalPermissions? ownGlobals = existingData?.ownGlobalPerms;
        ClientPairPermissions? ownPairPerms = existingData?.ownPairPermissions;
        ClientPairPermissionAccess? ownPairPermsAccess = existingData?.ownPairPermissionAccess;
        UserGlobalPermissions? otherGlobals = existingData?.otherGlobalPerms;
        ClientPairPermissions? otherPairPerms = existingData?.otherPairPermissions;
        ClientPairPermissionAccess? otherPairPermsAccess = existingData?.otherPairPermissionAccess;
        // Handle OwnGlobals
        if (ownGlobals is null)
        {
            UserGlobalPermissions? existingOwnGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if (existingOwnGlobals is null)
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
            ClientPairPermissions? existingOwnPairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if (existingOwnPairPerms is null)
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
            ClientPairPermissionAccess? existingOwnPairPermsAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if (existingOwnPairPermsAccess is null)
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
            UserGlobalPermissions? existingOtherGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if (existingOtherGlobals is null)
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
            ClientPairPermissions? existingOtherPairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID && p.OtherUserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if (existingOtherPairPerms is null)
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
            ClientPairPermissionAccess? existingOtherPairPermsAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID && p.OtherUserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
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
        GlobalPerms ownGlobalsApi = ownGlobals.ToApiGlobalPerms();
        PairPerms ownPairPermsApi = ownPairPerms.ToApiKinksterPerms();
        PairPermAccess ownPairPermsAccessApi = ownPairPermsAccess.ToApiKinksterEditAccess();
        GlobalPerms otherGlobalsApi = otherGlobals.ToApiGlobalPerms();
        PairPerms otherPairPermsApi = otherPairPerms.ToApiKinksterPerms();
        PairPermAccess otherPairPermsAccessApi = otherPairPermsAccess.ToApiKinksterEditAccess();

        // construct a new UserPairDto based on the response
        KinksterPair pairRequestAcceptingUserResponse = new(pairRequesterUser.ToUserData(), ownPairPermsApi,
            ownPairPermsAccessApi, otherGlobalsApi, otherPairPermsApi, otherPairPermsAccessApi);

        // inform the client caller's user that the pair was added successfully, to add the pair to their pair manager.
        KinksterRequestEntry removeRequest = new(new(existingRequest.UserUID), new(existingRequest.OtherUserUID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Callback_RemovePairRequest(removeRequest).ConfigureAwait(false);
        await Clients.User(pairRequestAcceptingUser.UID).Callback_AddClientPair(pairRequestAcceptingUserResponse).ConfigureAwait(false);

        // check if other user is online
        string? otherIdent = await GetUserIdent(pairRequesterUser.UID).ConfigureAwait(false);
        // do not send update to other user if they are not online.
        if (otherIdent is null)
            return HubResponseBuilder.Yippee();

        KinksterPair pairRequesterUserResponse = new(pairRequestAcceptingUser.ToUserData(), otherPairPermsApi,
            otherPairPermsAccessApi, ownGlobalsApi, ownPairPermsApi, ownPairPermsAccessApi);

        // They are online, so let them know to add the client pair to their pair manager.
        await Clients.User(pairRequesterUser.UID).Callback_RemovePairRequest(removeRequest).ConfigureAwait(false);
        await Clients.User(pairRequesterUser.UID).Callback_AddClientPair(pairRequesterUserResponse).ConfigureAwait(false);

        await Clients.User(UserUID).Callback_KinksterOnline(new(pairRequesterUser.ToUserData(), otherIdent)).ConfigureAwait(false);
        await Clients.User(pairRequesterUser.UID).Callback_KinksterOnline(new(pairRequestAcceptingUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRejectKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // grab the existing request from the database.
        KinksterRequest? existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == dto.User.UID && k.OtherUserUID == UserUID).ConfigureAwait(false);
        if (existingRequest is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot reject Kinkster Request from {dto.User.UID}, the request does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);
        }

        // send to both users to remove the kinkster request.
        KinksterRequestEntry rejectionDto = new(new(existingRequest.UserUID), new(existingRequest.OtherUserUID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Callback_RemovePairRequest(rejectionDto).ConfigureAwait(false);

        // send it to the other person if they are online at the time as well.
        string? otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent is not null)
        {
            await Clients.User(dto.User.UID).Callback_RemovePairRequest(rejectionDto).ConfigureAwait(false);
        }

        // remove the request from the database.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRemoveKinkster(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Dont allow removing self
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // See if clientPair exists at all in the database
        ClientPair? callerPair = await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (callerPair is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot remove {dto.User.UID} from your client pair list, the pair does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Get pair info of the user we are removing
        UserInfo? pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

        // remove the client pair from the database and all associated permissions. And then update changes
        DbContext.ClientPairs.Remove(callerPair);
        if (pairData?.ownPairPermissions is not null) DbContext.ClientPairPermissions.Remove(pairData.ownPairPermissions);
        if (pairData?.ownPairPermissionAccess is not null) DbContext.ClientPairPermissionAccess.Remove(pairData.ownPairPermissionAccess);
        // remove the other user's permissions as well.
        // grab the clientPairs item for the other direction.
        ClientPair? otherPair = await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == dto.User.UID && w.OtherUserUID == UserUID).ConfigureAwait(false);
        if (otherPair is not null)
        {
            DbContext.ClientPairs.Remove(otherPair);
            if (pairData?.otherPairPermissions is not null) DbContext.ClientPairPermissions.Remove(pairData.otherPairPermissions);
            if (pairData?.otherPairPermissionAccess is not null) DbContext.ClientPairPermissionAccess.Remove(pairData.otherPairPermissionAccess);
        }
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));
        // return to the client callers callback functions that we should remove them from the client callers pair manager.
        await Clients.User(UserUID).Callback_RemoveClientPair(dto).ConfigureAwait(false);

        // Check if the other user is online.
        string? otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent is null)
            return HubResponseBuilder.Yippee();

        // if they are, we should ask them to remove the client pair from thier listing as well.
        await Clients.User(dto.User.UID).Callback_RemoveClientPair(new(new(UserUID))).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveGag(PushKinksterActiveGagSlot dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        UserGagData? currentGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (currentGagData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

        GagType previousGag = currentGagData.Gag;
        Padlocks previousPadlock = currentGagData.Padlock;

        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyGags)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (currentGagData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                currentGagData.Gag = dto.Gag;
                currentGagData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                if (currentGagData.Gag is GagType.None)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (currentGagData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                if (!pairPerms.LockGags)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                // do a final validation pass.
                GagSpeakApiEc finalLockPass = currentGagData.CanLock(dto, pairPerms.MaxGagTime);
                if (finalLockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalLockPass);

                currentGagData.Padlock = dto.Padlock;
                currentGagData.Password = dto.Password;
                currentGagData.Timer = dto.Timer;
                currentGagData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (currentGagData.Gag is GagType.None)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                else if (!currentGagData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemNotLocked);

                else if (!pairPerms.UnlockGags)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                // validate if we can unlock the gag, if not, throw a warning.
                GagSpeakApiEc finalUnlockPass = currentGagData.CanUnlock(dto.Target.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalUnlockPass);

                currentGagData.Padlock = Padlocks.None;
                currentGagData.Password = string.Empty;
                currentGagData.Timer = DateTimeOffset.MinValue;
                currentGagData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                if (!pairPerms.RemoveGags)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (currentGagData.Gag is GagType.None)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (currentGagData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                currentGagData.Gag = GagType.None;
                currentGagData.Enabler = string.Empty;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Obtain the recipients to send the data to now that we know we can.
        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        ActiveGagSlot newGagData = currentGagData.ToApiGagSlot();
        KinksterUpdateActiveGag recipientDto = new(new(dto.User.UID), new(UserUID), newGagData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        // send to recipient.
        await Clients.User(dto.User.UID).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        // send back to all recipients pairs. (including client caller)
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveRestriction(PushKinksterActiveRestriction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        UserRestrictionData? curRestrictionData = await DbContext.UserRestrictionData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curRestrictionData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

        Guid prevRestriction = curRestrictionData.Identifier;
        Padlocks prevPadlock = curRestrictionData.Padlock;
        // Extra validation checks made on the server for security reasons.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyRestrictions)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (curRestrictionData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                curRestrictionData.Identifier = dto.RestrictionId;
                curRestrictionData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (curRestrictionData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                if (!pairPerms.LockRestrictions)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                // do a final validation pass.
                GagSpeakApiEc finalLockPass = curRestrictionData.CanLock(dto, pairPerms.MaxRestrictionTime);
                if (finalLockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalLockPass);


                curRestrictionData.Padlock = dto.Padlock;
                curRestrictionData.Password = dto.Password;
                curRestrictionData.Timer = dto.Timer;
                curRestrictionData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                else if (!curRestrictionData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemNotLocked);

                else if (!pairPerms.UnlockRestrictions)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                GagSpeakApiEc finalUnlockPass = curRestrictionData.CanUnlock(dto.Target.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalUnlockPass);

                curRestrictionData.Padlock = Padlocks.None;
                curRestrictionData.Password = string.Empty;
                curRestrictionData.Timer = DateTimeOffset.MinValue;
                curRestrictionData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (curRestrictionData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                if (!pairPerms.RemoveRestrictions)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                curRestrictionData.Identifier = Guid.Empty;
                curRestrictionData.Enabler = string.Empty;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        ActiveRestriction newRestrictionData = curRestrictionData.ToApiRestrictionSlot();
        KinksterUpdateActiveRestriction recipientDto = new(new(dto.User.UID), new(UserUID), newRestrictionData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevRestriction,
            PreviousPadlock = prevPadlock
        };

        await Clients.User(dto.User.UID).Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveRestraint(PushKinksterActiveRestraint dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        UserRestraintData? curRestraintSetData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (curRestraintSetData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        var prevRestraintSet = curRestraintSetData.Identifier;
        var prevLayers = curRestraintSetData.ActiveLayers;
        var prevPadlock = curRestraintSetData.Padlock;

        // Extra validation checks made on the server for security reasons.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyRestraintSets)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                curRestraintSetData.Identifier = dto.ActiveSetId;
                curRestraintSetData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.LayersChanged:
                if (curRestraintSetData.Identifier == Guid.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (!pairPerms.ApplyLayers || !pairPerms.RemoveLayers)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if ((!pairPerms.ApplyLayersWhileLocked || !pairPerms.RemoveLayersWhileLocked) && curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (curRestraintSetData.Padlock.IsDevotionalLock() && !UserUID.Equals(curRestraintSetData.PadlockAssigner))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // We are allowed, perform the update.
                curRestraintSetData.ActiveLayers = dto.ActiveLayers;
                break;

            case DataUpdateType.LayersApplied:
                if (curRestraintSetData.Identifier == Guid.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (!pairPerms.ApplyLayers)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.ApplyLayersWhileLocked && curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (curRestraintSetData.Padlock.IsDevotionalLock() && !UserUID.Equals(curRestraintSetData.PadlockAssigner))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // We are allowed, perform the update.
                var layersAdded = dto.ActiveLayers & ~prevLayers;
                curRestraintSetData.ActiveLayers |= layersAdded;
                break;

            case DataUpdateType.Locked:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                if (!pairPerms.LockRestraintSets)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                // do a final validation pass.
                GagSpeakApiEc finalLockPass = curRestraintSetData.CanLock(dto, pairPerms.MaxRestrictionTime);
                if (finalLockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalLockPass);

                curRestraintSetData.Padlock = dto.Padlock;
                curRestraintSetData.Password = dto.Password;
                curRestraintSetData.Timer = dto.Timer;
                curRestraintSetData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                else if (!curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemNotLocked);

                else if (!pairPerms.UnlockRestraintSets)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                GagSpeakApiEc finalUnlockPass = curRestraintSetData.CanUnlock(dto.Target.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GagSpeakApiEc.Success)
                    return HubResponseBuilder.AwDangIt(finalUnlockPass);

                curRestraintSetData.Padlock = Padlocks.None;
                curRestraintSetData.Password = string.Empty;
                curRestraintSetData.Timer = DateTimeOffset.MinValue;
                curRestraintSetData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.LayersRemoved:
                if (curRestraintSetData.Identifier == Guid.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (!pairPerms.RemoveLayers)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!pairPerms.RemoveLayersWhileLocked && curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (curRestraintSetData.Padlock.IsDevotionalLock() && !UserUID.Equals(curRestraintSetData.PadlockAssigner))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // We are allowed, perform the update.
                var layersRemoved = prevLayers & ~dto.ActiveLayers;
                curRestraintSetData.ActiveLayers &= ~layersRemoved;
                break;

            case DataUpdateType.Removed:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NoActiveItem);

                if (curRestraintSetData.IsLocked())
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ItemIsLocked);

                if (!pairPerms.RemoveRestraintSets)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                curRestraintSetData.Identifier = Guid.Empty;
                curRestraintSetData.Enabler = string.Empty;
                curRestraintSetData.ActiveLayers = RestraintLayer.None;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        CharaActiveRestraint updatedWardrobeData = curRestraintSetData.ToApiRestraintData();
        KinksterUpdateActiveRestraint recipientDto = new(dto.User, new(UserUID), updatedWardrobeData, dto.Type)
        {
            PreviousRestraint = prevRestraintSet,
            PrevLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.User(dto.User.UID).Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActivePattern(PushKinksterActivePattern dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // validate change over server.
        switch (dto.Type)
        {
            case DataUpdateType.PatternSwitched:
            case DataUpdateType.PatternExecuted:
                if (!pairPerms.ExecutePatterns)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                break;

            case DataUpdateType.PatternStopped:
                if (!pairPerms.StopPatterns)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // Grabs all Pairs of the affected pair
        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        await Clients.Users([ ..callbackUids, dto.Target.UID]).Callback_KinksterUpdateActivePattern(new(dto.Target, new(UserUID), dto.ActivePattern, dto.Type)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveAlarms(PushKinksterActiveAlarms dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        if (dto.Type is not DataUpdateType.AlarmToggled)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        if (!pairPerms.ToggleAlarms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        // Grabs all Pairs of the affected pair
        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        await Clients.Users([ ..callbackUids, dto.Target.UID ]).Callback_KinksterUpdateActiveAlarms(new(dto.Target, new(UserUID), dto.ActiveAlarms, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveTriggers(PushKinksterActiveTriggers dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        if (dto.Type is not DataUpdateType.TriggerToggled)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);

        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        if (!pairPerms.ToggleTriggers)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        // Grabs all Pairs of the affected pair
        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        await Clients.Users([.. callbackUids, dto.Target.UID]).Callback_KinksterUpdateActiveTriggers(new(dto.Target, new(UserUID), dto.ActiveTriggers, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendNameToKinkster(KinksterBase dto, string listenerName)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // verify that a pair between the two clients is made.
        ClientPair? pairPerms = await DbContext.ClientPairs.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // send the TARGET KINKSTER OUR USERDATA AND NAME.
        await Clients.User(dto.User.UID).Callback_ListenerName(new(UserUID), listenerName).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOtherGlobalPerm(SingleChangeGlobal dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Make sure the UserData within is for ourselves, since we called the [UpdateOwnGlobalPerm]
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Don't modify own perms on a UpdateOther call").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // fetch the global permission table row belonging to the user in the Dto
        UserGlobalPermissions? perms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Attempt to make the change.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Failed to set property to new Value!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);
        }

        // update the database with the new global permission & save DB changes
        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs that the paired user we are updating has.
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;
        // send callback to all the paired users of the userpair we modified, informing them of the update (includes the client caller)
        await Clients.Users(callbackUids).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        // finally, send a callback to the client pair who just had their permissions updated.
        await Clients.User(dto.User.UID).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOtherPairPerm(SingleChangeUnique dto)
    {
        // no way to verify if we are using it properly, so just make the assumption that we are.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // grab the pair permission row belonging to the paired user so we can modify it.
        ClientPairPermissions? perms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Attempt to make the change.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Failed to set property to new Value!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);
        }

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // inform the userpair we modified to update their own permissions
        await Clients.User(dto.User.UID).Callback_SingleChangeUnique(new(new(UserUID), dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        // inform the client caller to update the modified userpairs permission
        await Clients.Caller.Callback_SingleChangeUnique(new(dto.User, dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]

    /// <summary>
    ///     Sends a custom Hypnosis effect to another kinkster, which they will execute. <para />
    ///     If the image string is not null, ensure they have permission rights to do so.
    /// </summary>
    /// <remarks> Permission validation is not set yet for self-call test loops. </remarks>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserHypnotizeKinkster(HypnoticAction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot send a shock request to yourself! (yet?)").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // make sure they are added as a pair of the client caller.
        var pairGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (pairGlobals is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Global Perms do not exist!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairGlobals is null || pairPerms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "User is not paired with you").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Validate permission for image here down the line.
        if (dto.base64Image is not null && !pairPerms.AllowHypnoImageSending)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        // Fail if currently rendering a hypnosis effect.
        if (pairGlobals.HypnoState())
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.DuplicateEntry);

        // Otherwise we can set it (assuming the permissions are valid) and should also inform all of this pair's pairs.
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        // update the global permission value.
        pairGlobals.HypnosisCustomEffect = UserUID;
        DbContext.Update(pairGlobals);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send the globals update to the correct people.
        var newChange = new KeyValuePair<string, object>(nameof(GlobalPerms.HypnosisCustomEffect), UserUID);
        await Clients.Users(callbackUids).Callback_SingleChangeGlobal(new(dto.User, newChange, UpdateDir.Other, new(UserUID))).ConfigureAwait(false);
        // this will indirectly update their global permission we are changing as well, as we pass the UserUID into the callback.
        await Clients.User(dto.User.UID).Callback_HypnoticEffect(new(new(UserUID), dto.Duration, dto.Effect, dto.base64Image)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///     Instructs a Kinkster to be defined by a set address. <para />
    /// 
    ///     Because there is no way to know if the pair's IPC is valid outside of allowed permissions,
    ///     the operation will attmept, and fallback to nearest node. <para />
    ///     
    ///     If the task throws an exception due to an IPC failure or no nearest node being available, the
    ///     setting will be reverted by the target.
    /// </summary>
    /// <remarks> Permission validation is not set yet for self-call test loops. </remarks>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserConfineKinksterByAddress(ConfineByAddress dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot send a shock request to yourself! (yet?)").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // make sure they are added as a pair of the client caller.
        var pairGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (pairGlobals is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Global Perms do not exist!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairGlobals is null || pairPerms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "User is not paired with you").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Fail if in a confinement task already, or we are already confined.
        if (pairGlobals.HcConfinedState())
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Otherwise we can set it (assuming the permissions are valid) and should also inform all of this pair's pairs.
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        // update the global permission value. (add devotional pairlock here if nessisary)
        pairGlobals.IndoorConfinement = UserUID;
        DbContext.Update(pairGlobals);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send the globals update to the correct people.
        var newPerm = new KeyValuePair<string, object>(nameof(GlobalPerms.HypnosisCustomEffect), UserUID);
        await Clients.Users(callbackUids).Callback_SingleChangeGlobal(new(dto.User, newPerm, UpdateDir.Other, new(UserUID))).ConfigureAwait(false);
        // this will indirectly update their global permission we are changing as well, as we pass the UserUID into the callback.
        await Clients.User(dto.User.UID).Callback_ConfineToAddress(new(new(UserUID), dto.SpesificAddress)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///     Instructs a Kinkster to be anchors to a defined position. <para />
    ///     The Kinkster will not be able to stray further than the dto's defined radius. <para />
    ///     If the Kinkster is currently in a confinement task, this should fail.
    /// </summary>
    /// <remarks> Permission validation is not set yet for self-call test loops. </remarks>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserImprisonKinkster(ImprisonAtPosition dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot send a shock request to yourself! (yet?)").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // make sure they are added as a pair of the client caller.
        var pairGlobals = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (pairGlobals is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Global Perms do not exist!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairGlobals is null || pairPerms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "User is not paired with you").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        }

        // Fail if in a confinement task already, or we are already confined.
        if (pairGlobals.HcCageState())
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Otherwise we can set it (assuming the permissions are valid) and should also inform all of this pair's pairs.
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);
        IEnumerable<string> callbackUids = pairsOfClient.Keys;

        // update the global permission value. (add devotional pairlock here if nessisary)
        pairGlobals.Imprisonment = UserUID;
        DbContext.Update(pairGlobals);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send the globals update to the correct people.
        var newPerm = new KeyValuePair<string, object>(nameof(GlobalPerms.Imprisonment), UserUID);
        await Clients.Users(callbackUids).Callback_SingleChangeGlobal(new(dto.User, newPerm, UpdateDir.Other, new(UserUID))).ConfigureAwait(false);
        // this will indirectly update their global permission we are changing as well, as we pass the UserUID into the callback.
        await Clients.User(dto.User.UID).Callback_ImprisonAtPosition(new(new(UserUID), dto.Position, dto.MaxRadiusAllowed)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

}