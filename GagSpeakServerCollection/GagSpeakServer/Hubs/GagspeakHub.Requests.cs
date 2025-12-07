using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendKinksterRequest(CreateKinksterRequest dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        string uid = dto.User.UID.Trim();

        // return invalid if the user we wanna add is not in the database.
        User? otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false);
        if (otherUser is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, $"Cannot send Kinkster Request to {dto.User.UID}, the UID does not exist").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // if this "otherUser" is ourselves, return invalid.
        if (string.Equals(otherUser.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        bool existingPair = await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == otherUser.UID) || (p.UserUID == otherUser.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingPair)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPaired);

        bool existingRequest = await DbContext.KinksterPairRequests.AnyAsync(k => (k.UserUID == UserUID && k.OtherUserUID == otherUser.UID) || (k.UserUID == otherUser.UID && k.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingRequest)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestExists);

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
        KinksterPairRequest newDto = new(user.ToUserData(), otherUser.ToUserData(), dto.Message, newRequest.CreationTime);
        await Clients.User(UserUID).Callback_AddPairRequest(newDto).ConfigureAwait(false);

        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
            await Clients.User(otherUser.UID).Callback_AddPairRequest(newDto).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsCreated);
        _metrics.IncGauge(MetricsAPI.GaugePendingKinksterRequests);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserCancelKinksterRequest(KinksterBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure that the user we want to cancel a request to is not ourselves.
        string uid = dto.User.UID.Trim();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // return invalid if the user we wanna add is not in the database.
        User? otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid).ConfigureAwait(false);
        if (otherUser is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // if the existing entry was removed or no longer exists, notify them it was expired.
        KinksterRequest? existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == otherUser.UID).ConfigureAwait(false);
        if (existingRequest is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);

        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // notify the client caller and the recipient that the request was cancelled.
        var callbackDto = PermissionsEx.PairRequestRemoval(user.ToUserData(), otherUser.ToUserData());
        await Clients.User(UserUID).Callback_RemovePairRequest(callbackDto).ConfigureAwait(false);

        // send off to the other user if they are online.
        if (await GetUserIdent(otherUser.UID).ConfigureAwait(false) is { } otherIdent) 
            await Clients.User(otherUser.UID).Callback_RemovePairRequest(callbackDto).ConfigureAwait(false);

        // remove request from db and return.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
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
        User? pairRequesterUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == dto.User.UID).ConfigureAwait(false);
        if (pairRequesterUser is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // verify that the request exists in the database. (the pairRequesterUser would be the UserUID, we are OtherUserUID)
        KinksterRequest? existingRequest = await DbContext.KinksterPairRequests.SingleOrDefaultAsync(k => k.UserUID == pairRequesterUser.UID && k.OtherUserUID == UserUID).ConfigureAwait(false);
        if (existingRequest is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.KinksterRequestNotFound);

        // ensure that the client pair entry is not already existing.
        ClientPair? existingEntry = await DbContext.ClientPairs.AsNoTracking().FirstOrDefaultAsync(p => p.User.UID == UserUID && p.OtherUserUID == pairRequesterUser.UID).ConfigureAwait(false);
        if (existingEntry is not null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.AlreadyPaired);

        // Establish a new ClientPair relationship between the two users.
        User pairRequestAcceptingUser = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        ClientPair callerToRecipient = new ClientPair() { User = pairRequestAcceptingUser, OtherUser = pairRequesterUser, };
        ClientPair recipientToCaller = new ClientPair() { User = pairRequesterUser, OtherUser = pairRequestAcceptingUser, };

        await DbContext.ClientPairs.AddAsync(callerToRecipient).ConfigureAwait(false);
        await DbContext.ClientPairs.AddAsync(recipientToCaller).ConfigureAwait(false);

        // Obtain ALL relevant information about the relationship between these pairs.
        // This includes their current global perms, pair perms, and pair perms access.
        // If none are present, creates new versions.
        UserInfo? existingData = await GetPairInfo(UserUID, pairRequesterUser.UID).ConfigureAwait(false);

        // store the existing data permission items to objects for setting if null.
        UserGlobalPermissions? ownGlobals = existingData?.ownGlobalPerms;
        UserHardcoreState? ownHardcore = existingData?.ownHardcoreState;
        ClientPairPermissions? ownPairPerms = existingData?.ownPairPermissions;
        ClientPairPermissionAccess? ownPairPermsAccess = existingData?.ownPairPermissionAccess;
        UserGlobalPermissions? otherGlobals = existingData?.otherGlobalPerms;
        UserHardcoreState? otherHardcore = existingData?.otherHardcoreState;
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
        // Handle OwnHardcore
        if (ownHardcore is null)
        {
            UserHardcoreState? existingOwnHardcore = await DbContext.UserHardcoreState.SingleOrDefaultAsync(p => p.UserUID == pairRequestAcceptingUser.UID).ConfigureAwait(false);
            if (existingOwnHardcore is null)
            {
                ownHardcore = new UserHardcoreState() { User = pairRequestAcceptingUser };
                await DbContext.UserHardcoreState.AddAsync(ownHardcore).ConfigureAwait(false);
            }
            else
            {
                DbContext.UserHardcoreState.Update(existingOwnHardcore);
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
        // Handle OwnPermsAccess
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
        // Handle OtherGlobals
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
        // Handle OtherHardcore
        if (otherHardcore is null)
        {
            UserHardcoreState? existingOtherHardcore = await DbContext.UserHardcoreState.SingleOrDefaultAsync(p => p.UserUID == pairRequesterUser.UID).ConfigureAwait(false);
            if (existingOtherHardcore is null)
            {
                otherHardcore = new UserHardcoreState() { User = pairRequesterUser };
                await DbContext.UserHardcoreState.AddAsync(otherHardcore).ConfigureAwait(false);
            }
            else
            {
                DbContext.UserHardcoreState.Update(existingOtherHardcore);
            }
        }
        // Handle OtherPerms
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
        // Handle OtherPermsAccess
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
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // compile the api data objects.
        var ownGlobalsApi = ownGlobals.ToApiGlobalPerms();
        var ownHardcoreApi = ownHardcore.ToApiHardcoreState();
        var ownPermsApi = ownPairPerms.ToApiKinksterPerms();
        var ownPermAccessApi = ownPairPermsAccess.ToApiKinksterEditAccess();
        var otherGlobalsApi = otherGlobals.ToApiGlobalPerms();
        var otherHardcoreApi = otherHardcore.ToApiHardcoreState();
        var otherPermsApi = otherPairPerms.ToApiKinksterPerms();
        var otherPermAccessApi = otherPairPermsAccess.ToApiKinksterEditAccess();

        // construct a new UserPairDto based on the response
        KinksterPair pairRequestAcceptingUserResponse = new(pairRequesterUser.ToUserData(), ownPermsApi, ownPermAccessApi, 
            otherGlobalsApi, otherHardcoreApi, otherPermsApi, otherPermAccessApi);

        // inform the client caller's user that the pair was added successfully, to add the pair to their pair manager.
        var toRemove = PermissionsEx.PairRequestRemoval(new(existingRequest.UserUID), new(existingRequest.OtherUserUID));
        await Clients.User(UserUID).Callback_RemovePairRequest(toRemove).ConfigureAwait(false);
        await Clients.User(pairRequestAcceptingUser.UID).Callback_AddClientPair(pairRequestAcceptingUserResponse).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);

        // do not send update to other user if they are not online.
        if (await GetUserIdent(pairRequesterUser.UID).ConfigureAwait(false) is { } otherIdent)
        {
            KinksterPair pairRequesterUserResponse = new(pairRequestAcceptingUser.ToUserData(), otherPermsApi, otherPermAccessApi,
                ownGlobalsApi, ownHardcoreApi, ownPermsApi, ownPermAccessApi);

            // They are online, so let them know to add the client pair to their pair manager.
            await Clients.User(pairRequesterUser.UID).Callback_RemovePairRequest(toRemove).ConfigureAwait(false);
            await Clients.User(pairRequesterUser.UID).Callback_AddClientPair(pairRequesterUserResponse).ConfigureAwait(false);

            await Clients.User(UserUID).Callback_KinksterOnline(new(pairRequesterUser.ToUserData(), otherIdent)).ConfigureAwait(false);
            await Clients.User(pairRequesterUser.UID).Callback_KinksterOnline(new(pairRequestAcceptingUser.ToUserData(), UserCharaIdent)).ConfigureAwait(false);

        }
        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsAccepted);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
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
        KinksterPairRequest rejectionDto = new(new(existingRequest.UserUID), new(existingRequest.OtherUserUID), string.Empty, existingRequest.CreationTime);
        await Clients.User(UserUID).Callback_RemovePairRequest(rejectionDto).ConfigureAwait(false);

        if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is not null)
            await Clients.User(dto.User.UID).Callback_RemovePairRequest(rejectionDto).ConfigureAwait(false);

        // remove the request from the database.
        DbContext.KinksterPairRequests.Remove(existingRequest);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterKinksterRequestsRejected);
        _metrics.DecGauge(MetricsAPI.GaugePendingKinksterRequests);
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
        bool existingPair = await DbContext.ClientPairs.AnyAsync(p => (p.UserUID == UserUID && p.OtherUserUID == otherUser.UID) || (p.UserUID == otherUser.UID && p.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (!existingPair)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Request must not already exist.
        bool existingRequest = await DbContext.KinksterPairRequests.AnyAsync(k => (k.UserUID == UserUID && k.OtherUserUID == otherUser.UID) || (k.UserUID == otherUser.UID && k.OtherUserUID == UserUID)).ConfigureAwait(false);
        if (existingRequest)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.CollarRequestExists);

        // Obtain caller UserData.
        User user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // create new Collar Request, add to DB, then sync.
        GagspeakShared.Models.CollarRequest request = new GagspeakShared.Models.CollarRequest()
        {
            User = user,
            OtherUser = otherUser,
            CreationTime = DateTime.UtcNow,
            InitialWriting = dto.InitialWriting,
            OtherUserAccess = dto.UserAccess,
            OwnerAccess = dto.OwnerAccess,
        };
        await DbContext.CollarRequests.AddAsync(request).ConfigureAwait(false);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send back to both pairs that they have a new kinkster request.
        var callback = new GagspeakAPI.Network.CollarRequest(user.ToUserData(), otherUser.ToUserData(), request.InitialWriting, request.CreationTime, request.OtherUserAccess, request.OwnerAccess);
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
        if (await DbContext.KinksterPairRequests.AsNoTracking().SingleOrDefaultAsync(k => k.UserUID == UserUID && k.OtherUserUID == otherUser.UID).ConfigureAwait(false) is not { } request)
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
        DbContext.KinksterPairRequests.Remove(request);
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
        var recipientCollar = await DbContext.UserCollarData.SingleOrDefaultAsync(c => c.UserUID == UserUID).ConfigureAwait(false);
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
        DbContext.UserCollarData.Update(recipientCollar);
        
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
}