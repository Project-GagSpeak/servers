using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<KinksterRequest>> UserSendKinksterRequest(CreateKinksterRequest dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // The target to send the request to.
        string uid = dto.User.UID.Trim();

        // Prevent sending requests to self.
        if (string.Equals(uid, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.InvalidRecipient);

        // Prevent sending to a user not registered in Sundouleia.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false) is not { } target)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot send Request to {dto.User.UID}, they don't exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.InvalidRecipient);
        }

        // Sort the following calls by estimated tables with least entries to most entries for efficiency.
        if (await DbContext.PairRequests.AsNoTracking().AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == target.UID) || (p.UserUID == target.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.KinksterRequestExists);

        // Prevent sending a request if already paired.
        if (await DbContext.ClientPairs.AsNoTracking().AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == target.UID) || (p.UserUID == target.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.AlreadyPaired);

        // Request is valid for sending, so retrieve the context callers user model.
        var callerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // create a new KinksterPairRequest object, and add it to the database.
        PairRequest newRequest = new PairRequest()
        {
            User = callerUser,
            OtherUser = target,
            IsTemporary = dto.IsTemp,
            PreferredNickname = dto.PreferredNick,
            AttachedMessage = dto.Message,
            CreationTime = DateTime.UtcNow,
        };

        // append the request to the DB.
        await DbContext.PairRequests.AddAsync(newRequest).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Need to make a dto version of this that is sent back.
        var callbackDto = newRequest.ToApi();

        // If the target user's UID is in the redis DB, send them the pending request.
        if (await GetUserIdent(uid).ConfigureAwait(false) is not null)
            await Clients.User(uid).Callback_AddPairRequest(callbackDto).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsCreated);
        _metrics.IncGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee(callbackDto);
    }

    /// <summary>
    ///     If either the creator of a of the request cancels the request prior to its expiration time. <para />
    ///     Monitor this callback, if it returns successful, the caller should remove the request they wished to cancel.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserCancelKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // The target to send the request to.
        string uid = dto.User.UID.Trim();

        // Prevent sending requests to self.
        if (string.Equals(uid, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Prevent sending to a user not registered in Sundouleia.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false) is not { } target)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Ensure that the request does not already exist in the database.
        if (await DbContext.PairRequests.SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == target.UID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);

        // Can cancel the request:
        var callerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // Create the dummy callback for removal.
        var callbackDto = PermissionsEx.ToApiRemoval(new(UserUID), new(uid));

        // send off to the other user if they are online.
        if (await GetUserIdent(uid).ConfigureAwait(false) is { } otherIdent)
            await Clients.User(uid).Callback_RemovePairRequest(callbackDto).ConfigureAwait(false);

        // remove request from db and return.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    ///     Triggered whenever the recipient of a request accepts it. <para />
    ///     Bare in mind that due to the way this is called, the person accepting is the request entry <b>TARGET</b>. <para />
    ///     <b>If caller receives "AlreadyPaired", they should remove the request from their list.</b>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<AddedKinksterPair>> UserAcceptKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var uid = dto.User.UID.Trim();

        // Prevent accepting a request for self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.CannotInteractWithSelf);

        // Prevent sending to a user not registered in Sundouleia.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false) is not { } target)
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.NullData);

        // Ensure that the request does not already exist in the database.
        if (await DbContext.PairRequests.SingleOrDefaultAsync(k => k.UserUID == target.UID && k.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.KinksterRequestNotFound);

        // Do not consider temporary requests just yet.
        var wasTempRequest = request.IsTemporary;

        // Must not be already paired. If you are, discard the request regardless, but return with error. 
        if (await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == target.UID) || (p.UserUID == target.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false))
        {
            DbContext.PairRequests.Remove(request);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.AlreadyPaired);
        }

        // Request is properly accepted, create the relationship pair.
        var callerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        var callerToRecipient = new ClientPair()
        {
            User = callerUser,
            OtherUser = target,
            CreatedAt = DateTime.UtcNow,
            TempAccepterUID = string.Empty, // No Temporary requests for now.
        }; 
        var recipientToCaller = new ClientPair()
        {
            User = target,
            OtherUser = callerUser,
            CreatedAt = DateTime.UtcNow,
            TempAccepterUID = string.Empty, // No Temporary requests for now.
        };

        // Add them to the DB.
        await DbContext.ClientPairs.AddAsync(callerToRecipient).ConfigureAwait(false);
        await DbContext.ClientPairs.AddAsync(recipientToCaller).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // If the existing data is marked as null then we should abort as there is a serious issue going on.
        // This should only return null if they are not paired, but we just added that they are.
        var existingData = await GetPairInfo(UserUID, target.UID).ConfigureAwait(false);

        // We can reliably assume that everything but the pair permissions are valid at this point.
        var ownPerms = existingData?.OwnPerms;
        if (ownPerms is null)
        {
            ownPerms = new PairPermissions() { User = callerUser, OtherUser = target, };
            await DbContext.PairPermissions.AddAsync(ownPerms).ConfigureAwait(false);
        }

        var ownAccess = existingData?.OwnAccess;
        if (ownAccess is null)
        {
            ownAccess = new PairPermissionAccess() { User = callerUser, OtherUser = target, };
            await DbContext.PairAccess.AddAsync(ownAccess).ConfigureAwait(false);
        }

        var otherPerms = existingData?.OtherPerms;
        if (otherPerms is null)
        {
            otherPerms = new PairPermissions() { User = target, OtherUser = callerUser, };
            await DbContext.PairPermissions.AddAsync(otherPerms).ConfigureAwait(false);
        }

        var otherAccess = existingData?.OtherAccess;
        if (otherAccess is null)
        {
            otherAccess = new PairPermissionAccess() { User = target, OtherUser = callerUser, };
            await DbContext.PairAccess.AddAsync(otherAccess).ConfigureAwait(false);
        }

        // Finally, remove the request, then save the database.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Now compile together the KinksterPair data to return to the caller.
        // This is the KinksterPair of the caller (Request Accepter) -> Target (Request Creator)
        var callerRetDto = new KinksterPair(
            target.ToUserData(),
            ownPerms.ToApi(),
            ownAccess.ToApi(),
            existingData!.OtherGlobals.ToApi(),
            existingData!.OtherHcState.ToApi(),
            otherPerms.ToApi(),
            otherAccess.ToApi(),
            existingData.PairInitAt
        );

        // Get if the request creator is online or not.
        var requesterIdent = await GetUserIdent(target.UID).ConfigureAwait(false);

        // If the request creator is online, send them the remove request and add pair callbacks, and also return sendOnline to both.
        if (requesterIdent is not null)
        {
            var requesterRetDto = new KinksterPair(
                callerUser.ToUserData(),
                otherPerms.ToApi(),
                otherAccess.ToApi(),
                existingData!.OwnGlobals.ToApi(),
                existingData!.OwnHcState.ToApi(),
                ownPerms.ToApi(),
                otherAccess.ToApi(),
                existingData.PairInitAt
            );
            await Clients.User(uid).Callback_RemovePairRequest(PermissionsEx.ToApiRemoval(new(uid), new(UserUID))).ConfigureAwait(false);
            await Clients.User(uid).Callback_AddClientPair(requesterRetDto).ConfigureAwait(false);
            await Clients.User(uid).Callback_KinksterOnline(new(callerUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        }

        // Inc the metrics and then return result.
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);

        // Return the added kinkster pair to the caller as a responce.
        var retValue = new AddedKinksterPair(callerRetDto, requesterIdent != null ? new OnlineKinkster(target.ToUserData(), requesterIdent) : null);
        return HubResponseBuilder.Yippee(retValue);
    }

    /// <summary>
    ///     Whenever a pending request is rejected by the target recipient. <para />
    ///     You are expected to remove the request from your pending list if successful. (Helps save extra server calls)
    /// </summary>  
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRejectKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var requesterUid = dto.User.UID.Trim();

        // Prevent rejecting requests that do not exist.
        if (await DbContext.PairRequests.AsNoTracking().SingleOrDefaultAsync(r => r.UserUID == requesterUid && r.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);

        // See if we need to return the rejection request to the requester if they are online.
        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
        {
            KinksterRequest rejectedRequest = request.ToApi();
            await Clients.User(dto.User.UID).Callback_RemovePairRequest(rejectedRequest).ConfigureAwait(false);
        }

        // Remove from DB and save changes.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsRejected);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee();
    }

    // This is spesifically for when we are ready to implement temporary pairing.
    //[Authorize(Policy = "Identified")]
    //public async Task<HubResponse> UserPersistPair(KinksterBase dto)
    //{
    //    _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

    //    // If the pair does not yet exist, fail.
    //    if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairEntry)
    //        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

    //    // If the pair is not temporary, fail.
    //    if (string.IsNullOrEmpty(pairEntry.TempAccepterUID))
    //        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPermanent);

    //    // If the caller is not the temporary accepter, fail.
    //    if (!string.Equals(pairEntry.TempAccepterUID, UserUID, StringComparison.Ordinal))
    //        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

    //    // Make both sides permanent.
    //    var otherEntry = await DbContext.ClientPairs.SingleAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);

    //    pairEntry.TempAccepterUID = string.Empty;
    //    otherEntry.TempAccepterUID = string.Empty;

    //    // Update the DB.
    //    DbContext.ClientPairs.Update(pairEntry);
    //    DbContext.ClientPairs.Update(otherEntry);
    //    await DbContext.SaveChangesAsync().ConfigureAwait(false);

    //    // Notify the other user that they are now permanent if they are online.
    //    if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
    //        await Clients.User(dto.User.UID).Callback_PersistPair(new(new(UserUID))).ConfigureAwait(false);

    //    // return success to the caller, informing them they can update this pair to permanent.
    //    return HubResponseBuilder.Yippee();
    //}

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendCollarRequest(CreateCollarRequest dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        string uid = dto.User.UID.Trim();
        // Cannot be self.
        if (string.Equals(uid, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(uid))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // User must exist.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false) is not { } otherUser)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (!await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == otherUser.UID) || (p.UserUID == otherUser.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Request must not already exist.
        if (!await DbContext.PairRequests.AnyAsync(k => (k.UserUID == UserUID && k.OtherUserUID == otherUser.UID) || (k.UserUID == otherUser.UID && k.OtherUserUID == UserUID)).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CollarRequestExists);

        // Obtain caller UserData.
        var caller = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // create new Collar Request, add to DB, then sync.
        CollaringRequest request = new CollaringRequest()
        {
            User = caller,
            OtherUser = otherUser,
            CreationTime = DateTime.UtcNow,
            InitialWriting = dto.InitialWriting,
            OtherUserAccess = dto.UserAccess,
            OwnerAccess = dto.OwnerAccess,
        };
        await DbContext.CollarRequests.AddAsync(request).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send back to both pairs that they have a new kinkster request.
        var callback = new CollarRequest(caller.ToUserData(), otherUser.ToUserData(), request.InitialWriting, request.CreationTime, request.OtherUserAccess, request.OwnerAccess);
        await Clients.User(UserUID).Callback_AddCollarRequest(callback).ConfigureAwait(false);

        if (await GetUserIdent(otherUser.UID).ConfigureAwait(false) is { } otherIdent)
            await Clients.User(otherUser.UID).Callback_AddCollarRequest(callback).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterCollarRequestsCreated);
        _metrics.IncGauge(MetricsAPI.GaugePendingCollarRequests);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    // Cancelations come from the creator.
    public async Task<HubResponse> UserCancelCollarRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Cannot be self, kinkster dto must be valid string.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        
        // Kinkster Dto must exist. (If kinkster is deleted when request is sent, cleanup service removes it)
        if (await DbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UID == dto.User.UID).ConfigureAwait(false) is not { } otherUser)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Request with target as Caller UserUID and User as dto.User must exist.
        if (await DbContext.PairRequests.AsNoTracking().SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == otherUser.UID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CollarRequestNotFound);

        // Get User of person requesting change.
        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // get the request to remove as a dto.
        var toRemove = PermissionsEx.CollarRequestRemoval(user.ToUserData(), otherUser.ToUserData());
        await Clients.User(UserUID).Callback_RemoveCollarRequest(toRemove).ConfigureAwait(false);

        // send off to the other user if they are online.
        if (await GetUserIdent(otherUser.UID).ConfigureAwait(false) is { } otherIdent)
            await Clients.User(otherUser.UID).Callback_RemoveCollarRequest(toRemove).ConfigureAwait(false);

        // remove request from db and return.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        _metrics.DecGauge(MetricsAPI.GaugePendingCollarRequests);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    ///     The person being collared is the one accepting this, just FYI
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserAcceptCollarRequest(AcceptCollarRequest dto)
    {
        // dto User cannot be self, must be person who becomes our Owner.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Owner must exist.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == dto.User.UID).ConfigureAwait(false) is not { } newOwnerUser)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } clientPair)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Request must exist (Owner is dto.User, caller UserUID is OwnerUserUID).
        if (await DbContext.CollarRequests.AsNoTracking().SingleOrDefaultAsync(k => k.UserUID == dto.User.UID && k.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CollarRequestNotFound);

        // Get User of person requesting change.
        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        var recipientCollar = await DbContext.ActiveCollarData.SingleOrDefaultAsync(c => c.UserUID == UserUID).ConfigureAwait(false);
        if (recipientCollar is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // No active collar is present, if our dto contains null collar data, fail.
        if (dto.ChosenCollar is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        // no collar is actively present, so apply the preset data.
        recipientCollar.Visuals = true;
        recipientCollar.Dye1 = dto.ChosenCollar.Glamour.Dye1;
        recipientCollar.Dye2 = dto.ChosenCollar.Glamour.Dye2;
        recipientCollar.Writing = request.InitialWriting;
        DbContext.ActiveCollarData.Update(recipientCollar);
        
        // Regardless of the case, we should add the owner to it and update and save.
        var newOwner = new CollarOwner() { Owner = newOwnerUser, CollaredUserData = recipientCollar };
        await DbContext.CollarOwners.AddAsync(newOwner).ConfigureAwait(false);

        var toRemove = PermissionsEx.CollarRequestRemoval(user.ToUserData(), newOwnerUser.ToUserData());
        // Remove the request from the database.
        DbContext.CollarRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // obtain the latest CollarData API
        var collarApi = recipientCollar.ToApiCollarData();
        var newActive = new KinksterUpdateActiveCollar(user.ToUserData(), user.ToUserData(), collarApi, DataUpdateType.RequestAccepted);
        await Clients.Users([UserUID, newOwnerUser.UID]).Callback_RemoveCollarRequest(toRemove).ConfigureAwait(false);
        await Clients.Users([UserUID, newOwnerUser.UID]).Callback_KinksterUpdateActiveCollar(newActive).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterCollarRequestsAccepted);
        _metrics.DecGauge(MetricsAPI.GaugePendingCollarRequests);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")] // kinksterBase is the Owner who's request we are rejecting.
    public async Task<HubResponse> UserRejectCollarRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Reject if the request doesn't exist.
        if (await DbContext.CollarRequests.SingleOrDefaultAsync(k => k.UserUID == dto.User.UID && k.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CollarRequestNotFound);

        // remove the request from the database.
        DbContext.CollarRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Request can be rejected, so get a removal api message and push to both kinksters.
        var toRemove = PermissionsEx.CollarRequestRemoval(dto.User, new(UserUID));
        await Clients.Users([UserUID, dto.User.UID]).Callback_RemoveCollarRequest(toRemove).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterCollarRequestsRejected);
        _metrics.DecGauge(MetricsAPI.GaugePendingCollarRequests);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///     When the caller wishes to remove the specified user from their client pairs. <para />
    ///     If successful, you should remove the pair from your list of pairs.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRemoveKinkster(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Prevent removing self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Prevent processing if not paired.
        if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } callerPair)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Retrieve the additional info for the connection between the caller and target.
        UserInfo? pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

        // Remove the caller -> Target relation table entries.
        DbContext.ClientPairs.Remove(callerPair);
        if (pairData?.OwnPerms is not null) DbContext.PairPermissions.Remove(pairData.OwnPerms);
        if (pairData?.OwnAccess is not null) DbContext.PairAccess.Remove(pairData.OwnAccess);

        // Remove the target -> caller relation table entries.
        if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(w => w.UserUID == dto.User.UID && w.OtherUserUID == UserUID).ConfigureAwait(false) is { } otherPair)
        {
            DbContext.ClientPairs.Remove(otherPair);
            if (pairData?.OtherPerms is not null) DbContext.PairPermissions.Remove(pairData.OtherPerms);
            if (pairData?.OtherAccess is not null) DbContext.PairAccess.Remove(pairData.OtherAccess);
        }

        // Update DB.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // If the target is online, send to them the remove kinkster callback.
        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is { } otherIdent)
            await Clients.User(dto.User.UID).Callback_RemoveClientPair(new(new(UserUID))).ConfigureAwait(false); 

        return HubResponseBuilder.Yippee();
    }


    /// <summary> 
    ///     Method will delete the caller user profile from the database, and all associated data with it. <para />
    ///     If the caller's User entry is the primary account, then all secondary profiles are deleted with it.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserDelete()
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args());

        // Obtain the caller's Auth entry, which contains the User entry inside.
        if (await DbContext.Users.AsNoTracking().SingleOrDefaultAsync(a => a.UID == UserUID).ConfigureAwait(false) is not { } caller)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        var pairRemovals = await SharedDbFunctions.DeleteUserProfile(caller, _logger.Logger, DbContext, _metrics).ConfigureAwait(false);
        // send out to all the pairs to remove the deleted profile(s) from their lists.
        foreach (var (deletedProfile, profilePairUids) in pairRemovals)
            await Clients.Users(profilePairUids).Callback_RemoveClientPair(new(new(deletedProfile))).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }
}