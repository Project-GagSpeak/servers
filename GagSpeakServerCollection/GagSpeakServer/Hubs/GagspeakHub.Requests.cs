using GagspeakAPI.Connection;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.User;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<KinksterRequest>> UserCreatePairRequest(CreateRequest dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var uid = dto.User.UID.Trim();

        // Prevent sending requests to self.
        if (string.Equals(uid, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.InvalidRecipient);

        // Aquire both auths in one query.
        var authRecords = await DbContext.Auth.AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.UserUID == uid || a.User.Alias == uid || a.UserUID == UserUID)
            .ToListAsync().ConfigureAwait(false);

        if (authRecords.FirstOrDefault(a => string.Equals(a.UserUID, UserUID, StringComparison.Ordinal)) is not { } ownAuth)
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.NullData);

        if (authRecords.FirstOrDefault(a => string.Equals(a.UserUID, uid, StringComparison.Ordinal) || string.Equals(a.User.Alias, uid, StringComparison.Ordinal)) is not { } targetAuth)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot send Request to {dto.User.UID}, they don't exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.InvalidRecipient);
        }

        if (string.Equals(ownAuth.UserUID, targetAuth.UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.InvalidRecipient);

        // Evalutate relationship states between the users, so we reject if any are true.
        var relationState = await DbContext.Users
        .Where(u => u.UID == UserUID)
        .Select(u => new
        {
            HasExistingRequest = DbContext.PairRequests.Any(p => (p.UserUID == ownAuth.UserUID && p.OtherUserUID == targetAuth.UserUID) || (p.UserUID == targetAuth.UserUID && p.OtherUserUID == ownAuth.UserUID)),
            IsAlreadyPaired = DbContext.ClientPairs.Any(p => (p.UserUID == ownAuth.UserUID && p.OtherUserUID == targetAuth.UserUID) || (p.UserUID == targetAuth.UserUID && p.OtherUserUID == ownAuth.UserUID))
        })
        .FirstOrDefaultAsync().ConfigureAwait(false);

        if (relationState is null)
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.NullData);
        if (relationState.HasExistingRequest)
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.AlreadyExists);
        if (relationState.IsAlreadyPaired)
            return HubResponseBuilder.AwDangIt<KinksterRequest>(GagSpeakApiEc.AlreadyPaired);

        // Create and add the request to the DB, and save changes.
        var newRequest = new PairingRequest()
        {
            UserUID = ownAuth.UserUID,
            OtherUserUID = targetAuth.UserUID,
            IsTemporary = dto.IsTemp,
            AttachedMessage = dto.Message,
            CreationTime = DateTime.UtcNow,
        };
        await DbContext.PairRequests.AddAsync(newRequest).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Need to make a DTO version of this that is sent back.
        var callbackDto = newRequest.ToApi();

        // If the target user's UID is in the redis DB, send them the pending request.
        if (await GetUserIdent(targetAuth.UserUID).ConfigureAwait(false) is not null)
            await Clients.User(targetAuth.UserUID).Callback_AddPairRequest(callbackDto).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsCreated);
        _metrics.IncGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee(callbackDto);
    }

    /// <summary>
    ///   If either the creator of a of the request cancels the request prior to its expiration time. <para />
    ///   Monitor this callback, if it returns successful, the caller should remove the request they wished to cancel.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserCancelRequest(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // The target to send the request to.
        string uid = dto.User.UID.Trim();

        // Prevent sending requests to self.
        if (string.Equals(uid, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Prevent sending to a user not registered in Sundouleia.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false) is not { } target)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Ensure that the request does not already exist in the database.
        if (await DbContext.PairRequests.SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == target.UID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyExists);

        // Can cancel the request:
        var callerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // Create the dummy callback for removal.
        var callbackDto = Extensions.ToApiRemoval(new(UserUID), new(target.UID));

        // send off to the other user if they are online.
        if (await GetUserIdent(target.UID).ConfigureAwait(false) is { } otherIdent)
            await Clients.User(target.UID).Callback_RemovePairRequest(callbackDto).ConfigureAwait(false);

        // remove request from db and return.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserCancelRequests(UserListDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var targetUids = dto.Users.Select(u => u.UID.Trim()).ToList();

        // Fetch all request information in bulk using psql query language.
        var dbRequests = await GetSentRequests(targetUids).ConfigureAwait(false);

        // If none exist, return null data.
        if (dbRequests.Count == 0)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Prepare the callback DTO's for our callback notifications.
        var callbacks = dbRequests
            .Select(kvp => Extensions.ToApiRemoval(new(UserUID), new(kvp.Key)))
            .ToList();

        // Notify the online users.
        foreach (var callback in callbacks)
        {
            if (await GetUserIdent(callback.Target.UID).ConfigureAwait(false) is not null)
                await Clients.User(callback.Target.UID).Callback_RemovePairRequest(callback).ConfigureAwait(false);
        }

        // Remove all requests in bulk.
        DbContext.PairRequests.RemoveRange(dbRequests.Values);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests, dbRequests.Count);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    ///   Triggered whenever the recipient of a request accepts it. <para />
    ///   Bare in mind that due to the way this is called, the person accepting is the request entry <b>TARGET</b>. <para />
    ///   <b>If caller receives "AlreadyPaired", they should remove the request from their list.</b>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<AddedKinksterPair>> UserAcceptRequest(RequestResponse dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var uid = dto.User.UID.Trim();

        // Prevent accepting a request for self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.CannotInteractWithSelf);

        // Prevent sending to a user not registered in Sundouleia.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false) is not { } target)
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.NullData);

        // Ensure that the request does not already exist in the database.
        if (await DbContext.PairRequests.SingleOrDefaultAsync(k => k.UserUID == target.UID && k.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt<AddedKinksterPair>(GagSpeakApiEc.RequestNotFound);

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
            TempAccepterUID = dto.AsTemp ? callerUser.UID : string.Empty,
        };
        var recipientToCaller = new ClientPair()
        {
            User = target,
            OtherUser = callerUser,
            CreatedAt = DateTime.UtcNow,
            TempAccepterUID = dto.AsTemp ? callerUser.UID : string.Empty,
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
            existingData.PairInitAt,
            existingData.PairTempAccepter
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
                existingData.PairInitAt,
                existingData.PairTempAccepter
            );
            await Clients.User(target.UID).Callback_RemovePairRequest(PermissionsEx.ToApiRemoval(target.ToUserData(), new(UserUID))).ConfigureAwait(false);
            await Clients.User(target.UID).Callback_AddPair(requesterRetDto).ConfigureAwait(false);
            await Clients.User(target.UID).Callback_KinksterOnline(new(callerUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        }

        // Inc the metrics and then return result.
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);

        // Return the added kinkster pair to the caller as a responce.
        var retValue = new AddedKinksterPair(callerRetDto, requesterIdent != null ? new OnlineKinkster(target.ToUserData(), requesterIdent) : null);
        return HubResponseBuilder.Yippee(retValue);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<List<AddedKinksterPair>>> UserAcceptRequests(RequestResponses toAccept)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(toAccept));
        var senderUids = toAccept.Responses.Select(u => u.User.UID.Trim()).ToList();
        senderUids.Remove(UserUID); // Pre-fire removing self

        if (senderUids.Count is 0)
            return HubResponseBuilder.AwDangIt<List<AddedKinksterPair>>(GagSpeakApiEc.NullData);

        // Fetch valid requests from the DB
        var requests = await GetRespondingRequests(senderUids).ConfigureAwait(false);

        if (requests.Count is 0)
            return HubResponseBuilder.AwDangIt<List<AddedKinksterPair>>(GagSpeakApiEc.NullData);

        // Build what we are returning.
        var now = DateTime.UtcNow;
        var callerUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        var onlineUsers = await GetOnlineUsers(senderUids).ConfigureAwait(false);

        var addedPairs = new List<AddedKinksterPair>(requests.Count);
        // Process each request one by one, without extra queries.
        foreach (var (senderUid, info) in requests)
        {
            var request = info.Request;
            var acceptAsTemp = toAccept.Responses.Single(r => string.Equals(r.User.UID, senderUid, StringComparison.OrdinalIgnoreCase)).AsTemp;

            // Create pairs
            var callerToTarget = new ClientPair
            {
                UserUID = callerUser.UID,
                OtherUserUID = info.Sender.UID,
                CreatedAt = now,
                TempAccepterUID = acceptAsTemp ? UserUID : string.Empty
            };

            var targetToCaller = new ClientPair
            {
                UserUID = info.Sender.UID,
                OtherUserUID = callerUser.UID,
                CreatedAt = now,
                TempAccepterUID = acceptAsTemp ? UserUID : string.Empty
            };

            DbContext.ClientPairs.AddRange(callerToTarget, targetToCaller);

            // Create perms (always new in bulk accept)
            var ownPerms = new PairPermissions { User = callerUser, OtherUser = request.User };
            var ownAccess = new PairPermissionAccess { User = callerUser, OtherUser = request.User };
            var otherPerms = new PairPermissions { User = request.User, OtherUser = callerUser };
            var otherAccess = new PairPermissionAccess { User = request.User, OtherUser = callerUser };

            DbContext.PairPermissions.AddRange(ownPerms, otherPerms);
            DbContext.PairAccess.AddRange(ownAccess, otherAccess);
            DbContext.PairRequests.Remove(request);

            // Build return DTO (caller perspective)
            var callerRetDto = new KinksterPair(
                info.Sender.ToUserData(),
                ownPerms.ToApi(),
                ownAccess.ToApi(),
                info.SenderGlobals.ToApi(),
                info.SenderHC.ToApi(),
                otherPerms.ToApi(),
                otherAccess.ToApi(),
                DateTime.UtcNow,
                acceptAsTemp ? UserUID : string.Empty
            );

            // Inform other if online.
            if (onlineUsers.TryGetValue(senderUid, out var ident))
            {
                // Online user.
                addedPairs.Add(new(callerRetDto, new(info.Sender.ToUserData(), ident)));
                // Return for the request sender.
                var senderRetDto = new KinksterPair(
                    callerUser.ToUserData(),
                    otherPerms.ToApi(),
                    otherAccess.ToApi(),
                    info.RecipientGlobals.ToApi(),
                    info.RecipientHC.ToApi(),
                    ownPerms.ToApi(),
                    otherAccess.ToApi(),
                    DateTime.UtcNow,
                    acceptAsTemp ? UserUID : string.Empty
                );
                await Clients.User(senderUid).Callback_RemovePairRequest(Extensions.ToApiRemoval(new(senderUid), new(UserUID))).ConfigureAwait(false);
                await Clients.User(senderUid).Callback_AddPair(senderRetDto).ConfigureAwait(false);
                await Clients.User(senderUid).Callback_KinksterOnline(new(callerUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
            }
            // Otherwise, simply add.
            else
            {
                addedPairs.Add(new(callerRetDto, null));
            }
        }

        // Save the changes.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Update metrics and return.
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted, requests.Count);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests, requests.Count);

        return HubResponseBuilder.Yippee(addedPairs);
    }

    /// <summary>
    ///   Whenever a pending request is rejected by the target recipient. <para />
    ///   You are expected to remove the request from your pending list if successful. (Helps save extra server calls)
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRejectRequest(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var requesterUid = dto.User.UID.Trim();

        // Prevent rejecting requests that do not exist.
        if (await DbContext.PairRequests.AsNoTracking().SingleOrDefaultAsync(r => r.UserUID == requesterUid && r.OtherUserUID == UserUID).ConfigureAwait(false) is not { } request)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RequestNotFound);

        // See if we need to return the rejection request to the requester if they are online.
        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
        {
            var rejectedRequest = request.ToApi();
            await Clients.User(dto.User.UID).Callback_RemovePairRequest(rejectedRequest).ConfigureAwait(false);
        }

        // Remove from DB and save changes.
        DbContext.PairRequests.Remove(request);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsRejected);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRejectRequests(UserListDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var requesterUids = dto.Users.Select(u => u.UID.Trim()).ToHashSet(StringComparer.Ordinal);

        // Fetch all requests where the current user is the target.
        var dbRequests = await DbContext.PairRequests
            .AsNoTracking()
            .Where(r => r.OtherUserUID == UserUID && requesterUids.Contains(r.UserUID))
            .ToListAsync().ConfigureAwait(false);

        // If none exist, return not found, note that even if not all request exists,
        // the caller should still remove all after, as they no longer exist on the server.
        if (dbRequests.Count == 0)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RequestNotFound);

        // Notify each requester if they are online.
        foreach (var request in dbRequests)
        {
            if (await GetUserIdent(request.UserUID).ConfigureAwait(false) is not null)
                await Clients.User(request.UserUID).Callback_RemovePairRequest(request.ToApi()).ConfigureAwait(false);
        }

        // Remove all requests in bulk.
        DbContext.PairRequests.RemoveRange(dbRequests);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsRejected, dbRequests.Count);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests, dbRequests.Count);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPersistKinkster(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // If the pair does not yet exist, fail.
        if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(p => p.UserUID == UserUID && p.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairEntry)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // If the pair is not temporary, fail.
        if (string.IsNullOrEmpty(pairEntry.TempAccepterUID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPermanent);

        // If the caller is not the temporary accepter, fail.
        if (!string.Equals(pairEntry.TempAccepterUID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Make both sides permanent.
        var otherEntry = await DbContext.ClientPairs.SingleAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);

        pairEntry.TempAccepterUID = string.Empty;
        otherEntry.TempAccepterUID = string.Empty;

        // Update the DB.
        DbContext.ClientPairs.Update(pairEntry);
        DbContext.ClientPairs.Update(otherEntry);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Notify the other user that they are now permanent if they are online.
        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
            await Clients.User(dto.User.UID).Callback_PersistPair(new(new(UserUID))).ConfigureAwait(false);

        // return success to the caller, informing them they can update this pair to permanent.
        return HubResponseBuilder.Yippee();
    }

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
        var request = new CollaringRequest()
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
    public async Task<HubResponse> UserCancelCollarRequest(UserDto dto)
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
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

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
    ///   The person being collared is the one accepting this, just FYI
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserAcceptCollarRequest(CollarResponse dto)
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
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
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
    public async Task<HubResponse> UserRejectCollarRequest(UserDto dto)
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
    ///   When the caller wishes to remove the specified user from their client pairs. <para />
    ///   If successful, you should remove the pair from your list of pairs.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRemoveKinkster(UserDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Prevent removing self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Prevent processing if not paired.
        if (await DbContext.ClientPairs.AsNoTracking().SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } callerPair)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Retrieve the additional info for the connection between the caller and target.
        var pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

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
            await Clients.User(dto.User.UID).Callback_RemovePair(new(new(UserUID))).ConfigureAwait(false); 

        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserRemoveKinksters(UserListDto toRemove)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(toRemove));
        // Hashset for O(1) lookup efficiency.
        var targetUids = toRemove.Users.Select(u => u.UID.Trim()).ToHashSet(StringComparer.Ordinal);
        targetUids.Remove(UserUID);

        // "Then draw the rest of the picture"
        var pairsAndPerms = await (
            from up in DbContext.ClientPairs.AsNoTracking().Where(cp => cp.UserUID == UserUID && targetUids.Contains(cp.OtherUserUID))
            join tp in DbContext.ClientPairs.AsNoTracking().Where(cp => cp.OtherUserUID == UserUID && targetUids.Contains(cp.UserUID))
                on up.OtherUserUID equals tp.UserUID into joined
            from tp in joined.DefaultIfEmpty()
            join ownperm in DbContext.PairPermissions.AsNoTracking().Where(p => p.UserUID == UserUID && targetUids.Contains(p.OtherUserUID))
                on new { up.UserUID, up.OtherUserUID } equals new { ownperm.UserUID, ownperm.OtherUserUID } into ownperms
            from ownperm in ownperms.DefaultIfEmpty()
            join otherperm in DbContext.PairPermissions.AsNoTracking().Where(p => p.OtherUserUID == UserUID && targetUids.Contains(p.UserUID))
                on new { tp.UserUID, tp.OtherUserUID } equals new { otherperm.UserUID, otherperm.OtherUserUID } into otherperms
            from otherperm in otherperms.DefaultIfEmpty()
            join ownaccess in DbContext.PairAccess.AsNoTracking().Where(p => p.UserUID == UserUID && targetUids.Contains(p.OtherUserUID))
                on new { up.UserUID, up.OtherUserUID } equals new { ownaccess.UserUID, ownaccess.OtherUserUID } into ownaccesses
            from ownaccess in ownaccesses.DefaultIfEmpty()
            join otheraccess in DbContext.PairAccess.AsNoTracking().Where(p => p.OtherUserUID == UserUID && targetUids.Contains(p.UserUID))
                on new { tp.UserUID, tp.OtherUserUID } equals new { otheraccess.UserUID, otheraccess.OtherUserUID } into otheraccesses
            from otheraccess in otheraccesses.DefaultIfEmpty()

            select new
            {
                OtherUid = up.OtherUserUID,
                CallerPair = up,
                TargetPair = tp,
                OwnPerms = ownperm,
                OtherPerms = otherperm,
                OwnAccess = ownaccess,
                OtherAccess = otheraccess
            }
        ).ToListAsync().ConfigureAwait(false);

        if (pairsAndPerms.Count == 0)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Bulk Remove.
        DbContext.ClientPairs.RemoveRange(pairsAndPerms.Select(x => x.CallerPair));
        DbContext.ClientPairs.RemoveRange(pairsAndPerms.Where(x => x.TargetPair != null).Select(x => x.TargetPair!));
        DbContext.PairPermissions.RemoveRange(pairsAndPerms.Where(x => x.OwnPerms != null).Select(x => x.OwnPerms!));
        DbContext.PairPermissions.RemoveRange(pairsAndPerms.Where(x => x.OtherPerms != null).Select(x => x.OtherPerms!));
        DbContext.PairAccess.RemoveRange(pairsAndPerms.Where(x => x.OwnAccess != null).Select(x => x.OwnAccess!));
        DbContext.PairAccess.RemoveRange(pairsAndPerms.Where(x => x.OtherAccess != null).Select(x => x.OtherAccess!));
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Inform Online Users of the removal.
        if (pairsAndPerms.Count > 0)
            await Clients.Users(pairsAndPerms.Select(p => p.OtherUid)).Callback_RemovePair(new(new(UserUID))).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }


    /// <summary> 
    ///   When the caller wishes to commit seppuku to their profile. <para/>
    ///   If the profile being deleted is the account's primary profile, all secondary profiles are deleted with it.
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
            await Clients.Users(profilePairUids).Callback_RemovePair(new(new(deletedProfile))).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }
}