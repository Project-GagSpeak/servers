using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushData(PushClientCompositeUpdate dto)
    {
        _logger.LogCallInfo();

        // fetch the recipient UID list from the recipient list of the dto
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        // check to see if all the recipients are cached within the cache service. If not, then cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            // fetch all the paired users of the client caller
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            // see which of the paired users are in the recipient list, and cache them.
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // if a safeword, we need to clear all the data for the appearance and activeSetData.
        if (dto.WasSafeword)
        {
            // Grab the gag data ordered by layer.
            List<UserGagData> curGagData = await DbContext.UserGagData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            if (curGagData.Any(g => g is null))
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot clear Gag Data, it does not exist!").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
            }

            // Grab the restriction data ordered by layer.
            List<UserRestrictionData> curRestrictionData = await DbContext.UserRestrictionData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            if (curRestrictionData.Any(r => r is null))
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot clear Active Restriction Data, it does not exist!").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
            }

            // Grab the restraint set data.
            UserRestraintData? curRestraintData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curRestraintData is null)
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot clear Restraint Data, it does not exist!").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
            }

            // Clear gagData for all layers.
            foreach (UserGagData gagData in curGagData)
            {
                gagData.Gag = GagType.None;
                gagData.Enabler = string.Empty;
                gagData.Padlock = Padlocks.None;
                gagData.Password = string.Empty;
                gagData.Timer = DateTimeOffset.MinValue;
                gagData.PadlockAssigner = string.Empty;
            }

            // Clear restrictionData for all layers.
            foreach (UserRestrictionData activeSetData in curRestrictionData)
            {
                activeSetData.Identifier = Guid.Empty;
                activeSetData.Enabler = string.Empty;
                activeSetData.Padlock = Padlocks.None;
                activeSetData.Password = string.Empty;
                activeSetData.Timer = DateTimeOffset.MinValue;
                activeSetData.PadlockAssigner = string.Empty;
            }

            // Clear the restraintData.
            curRestraintData.Identifier = Guid.Empty;
            curRestraintData.Enabler = string.Empty;
            curRestraintData.Padlock = Padlocks.None;
            curRestraintData.Password = string.Empty;
            curRestraintData.Timer = DateTimeOffset.MinValue;
            curRestraintData.PadlockAssigner = string.Empty;

            // Dont need to update tables since they are using tracking. Just save changes.
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            // If a safeword, the contents of the composite data wont madder, since we know they are being reset and can handle it on the client end.
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
            await Clients.Users(recipientUids).Callback_KinksterUpdateComposite(new(new(UserUID), dto.NewData, dto.WasSafeword)).ConfigureAwait(false);
        }
        else
        {
            // Push the composite data off to the other pairs.
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
            await Clients.Users(recipientUids).Callback_KinksterUpdateComposite(new(new(UserUID), dto.NewData, dto.WasSafeword)).ConfigureAwait(false);
        }

        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataIpc(PushClientIpcUpdate dto)
    {
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        await Clients.Users(recipientUids).Callback_KinksterUpdateIpc(new(new(UserUID), new(UserUID), dto.NewData, dto.Type)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataGags(PushClientGagSlotUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Get who we are sending it to, and cache any pairs not currently synced with the pool.
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // Grab the appearance data from the database at the layer we want to interact with.
        UserGagData? curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curGagData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // get the previous gag type and padlock from the current data.
        GagType previousGag = curGagData.Gag;
        Padlocks previousPadlock = curGagData.Padlock;

        // we can always assume that this is correct when applied by self.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curGagData.Gag = dto.Gag;
                curGagData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                curGagData.Padlock = dto.Padlock;
                curGagData.Password = dto.Password;
                curGagData.Timer = dto.Timer;
                curGagData.PadlockAssigner = dto.Assigner;
                break;

            case DataUpdateType.Unlocked:
                curGagData.Padlock = Padlocks.None;
                curGagData.Password = string.Empty;
                curGagData.Timer = DateTimeOffset.UtcNow;
                curGagData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                curGagData.Gag = GagType.None;
                curGagData.Enabler = string.Empty;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated appearance.
        ActiveGagSlot newAppearance = curGagData.ToApiGagSlot();
        KinksterUpdateGagSlot recipientDto = new(new(UserUID), new(UserUID), newAppearance, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateGagSlot(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateGagSlot(recipientDto).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataRestrictions(PushClientRestrictionUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Get who we are sending it to, and cache any pairs not currently synced with the pool.
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        UserRestrictionData? curRestrictionData = await DbContext.UserRestrictionData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curRestrictionData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // get the previous gag type and padlock from the current data.
        Guid prevId = curRestrictionData.Identifier;
        Padlocks prevPadlock = curRestrictionData.Padlock;

        // we can always assume that this is correct when applied by self.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curRestrictionData.Identifier = dto.Identifier;
                curRestrictionData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                curRestrictionData.Padlock = dto.Padlock;
                curRestrictionData.Password = dto.Password;
                curRestrictionData.Timer = dto.Timer;
                curRestrictionData.PadlockAssigner = dto.Assigner;
                break;

            case DataUpdateType.Unlocked:
                curRestrictionData.Padlock = Padlocks.None;
                curRestrictionData.Password = string.Empty;
                curRestrictionData.Timer = DateTimeOffset.UtcNow;
                curRestrictionData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                curRestrictionData.Identifier = Guid.Empty;
                curRestrictionData.Enabler = string.Empty;
                break;


            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restrictionData.
        ActiveRestriction newRestrictionData = curRestrictionData.ToApiRestrictionSlot();
        KinksterUpdateRestriction recipientDto = new(new(UserUID), new(UserUID), newRestrictionData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevId,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateRestriction(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateRestriction(recipientDto).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataRestraint(PushClientRestraintUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // grab the restraintSetData from the database.
        UserRestraintData? curRestraintData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (curRestraintData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        Guid prevSetId = curRestraintData.Identifier;
        RestraintLayer prevLayers = curRestraintData.ActiveLayers;
        Padlocks prevPadlock = curRestraintData.Padlock;

        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curRestraintData.Identifier = dto.ActiveSetId;
                curRestraintData.Enabler = dto.Enabler;
                break;

            // No bitwise operations for right now, just raw updates.
            case DataUpdateType.LayersChanged:
            case DataUpdateType.LayersApplied:
            case DataUpdateType.LayersRemoved:
                curRestraintData.ActiveLayers = dto.ActiveLayers;
                break;

            case DataUpdateType.Locked:
                curRestraintData.Padlock = dto.Padlock;
                curRestraintData.Password = dto.Password;
                curRestraintData.Timer = dto.Timer;
                curRestraintData.PadlockAssigner = dto.Assigner;
                break;

            case DataUpdateType.Unlocked:
                curRestraintData.Padlock = Padlocks.None;
                curRestraintData.Password = string.Empty;
                curRestraintData.Timer = DateTimeOffset.UtcNow;
                curRestraintData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                curRestraintData.Identifier = Guid.Empty;
                curRestraintData.Enabler = string.Empty;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restraintData.
        CharaActiveRestraint newRestraintData = curRestraintData.ToApiRestraintData();
        KinksterUpdateRestraint recipientDto = new(new(UserUID), new(UserUID), newRestraintData, dto.Type)
        {
            PreviousRestraint = prevSetId,
            PrevLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateRestraint(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateRestraint(recipientDto).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataCursedLoot(PushClientCursedLootUpdate dto)
    {
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // Handle the cursed loot based on the type.
        if (dto.InteractedLoot.GagType is not GagType.None)
        {
            // Grab the user gag data associated with our name, and find the first layer that does not have a gag item. If none found, return error.
            UserGagData? curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(data => data.UserUID == UserUID && data.Gag == GagType.None).ConfigureAwait(false);
            if (curGagData is null)
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

            // get the previous gag.
            GagType previousGag = curGagData.Gag;

            // update the data.
            curGagData.Gag = dto.InteractedLoot.GagType;
            curGagData.Enabler = "Mimic";
            curGagData.Padlock = Padlocks.MimicPadlock;
            curGagData.Password = string.Empty;
            curGagData.Timer = dto.InteractedLoot.ReleaseTimeUTC;
            curGagData.PadlockAssigner = "Mimic";

            // save changes to our tracked item.
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            ActiveGagSlot newAppearance = curGagData.ToApiGagSlot();
            KinksterUpdateGagSlot recipientDto = new(new(UserUID), new(UserUID), newAppearance, DataUpdateType.AppliedCursed)
            {
                AffectedLayer = curGagData.Layer,
                PreviousGag = previousGag,
                PreviousPadlock = Padlocks.None
            };
            // Return Gag Update.
            await Clients.Users(recipientUids).Callback_KinksterUpdateGagSlot(recipientDto).ConfigureAwait(false);
            await Clients.Caller.Callback_KinksterUpdateGagSlot(recipientDto).ConfigureAwait(false);
        }

        // Push CursedLoot update to all recipients.
        await Clients.Users(recipientUids).Callback_KinksterUpdateCursedLoot(new(new(UserUID), dto.ActiveItems, dto.InteractedLoot)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushAliasGlobalUpdate(PushClientAliasGlobalUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // convert the recipient UID list from the recipient list of the dto
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        await Clients.Users(recipientUids).Callback_KinksterUpdateAliasGlobal(new(new(UserUID), dto.Alias)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushAliasUniqueUpdate(PushClientAliasUniqueUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        await Clients.User(dto.Recipient.UID).Callback_KinksterUpdateAliasGlobal(new(new(UserUID), dto.Alias)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataToybox(PushClientToyboxUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // we need to update our lists based on the interacted item, and store what item was interacted with.
        switch (dto.Type)
        {
            case DataUpdateType.PatternSwitched:
            case DataUpdateType.PatternExecuted:
                dto.LatestActiveItems.ActivePattern = dto.AffectedIdentifier;
                break;

            case DataUpdateType.PatternStopped:
                dto.LatestActiveItems.ActivePattern = Guid.Empty;
                break;

            case DataUpdateType.AlarmToggled:
                // If we couldnt remove it, it means it was not present, so add it.
                if (!dto.LatestActiveItems.ActiveAlarms.Remove(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveAlarms.Add(dto.AffectedIdentifier);
                break;

            case DataUpdateType.TriggerToggled:
                // If we couldnt remove it, it means it was not present, so add it.
                if (!dto.LatestActiveItems.ActiveTriggers.Remove(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveTriggers.Add(dto.AffectedIdentifier);
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        await Clients.Users(recipientUids).Callback_KinksterUpdateToybox(new(new(UserUID), new(UserUID), dto.LatestActiveItems, dto.Type)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserPushDataLightStorage(PushClientLightStorageUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        await Clients.Users(recipientUids).Callback_KinksterUpdateLightStorage(new(new(UserUID), new(UserUID), dto.NewData)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserBulkChangeGlobal(BulkChangeGlobal dto)
    {
        _logger.LogCallInfo();
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Can't Bulk update others besides yourself.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // fetch the user global perm from the database.
        UserGlobalPermissions? perms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // update the permissions to the new values passed in.
        UserGlobalPermissions newGlobalPerms = dto.NewPerms.ToModelGlobalPerms(perms);

        // update the database with the new permissions & save DB changes
        DbContext.Update(newGlobalPerms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

        await Clients.Caller.Callback_BulkChangeGlobal(new(new(UserUID), dto.NewPerms)).ConfigureAwait(false);
        await Clients.Users(pairsOfClient.Select(p => p.Key)).Callback_BulkChangeGlobal(new(new(UserUID), dto.NewPerms)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UserBulkChangeUnique(BulkChangeUnique dto)
    {
        _logger.LogCallInfo();
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Don't modify own perms on a UpdateOther call").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // grab our pair permissions for this user.
        ClientPairPermissions? pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (pairPerms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair Permission Not Found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }
        // grab the pair permission access for this user.
        ClientPairPermissionAccess? pairAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (pairAccess is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission access not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // Update the global permissions, pair permissions, and editAccess permissions with the new values.
        ClientPairPermissions newPairPerms = dto.NewPerms.ToModelKinksterPerms(pairPerms);
        ClientPairPermissionAccess newPairAccess = dto.NewAccess.ToModelKinksterEditAccess(pairAccess);

        // update the database with the new permissions & save DB changes
        DbContext.Update(newPairPerms);
        DbContext.Update(newPairAccess);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.Caller.Callback_BulkChangeUnique(new(dto.User, dto.NewPerms, dto.NewAccess)).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Callback_BulkChangeUnique(new(new(UserUID), dto.NewPerms, dto.NewAccess)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<HubResponse> UserChangeOwnGlobalPerm(SingleChangeGlobal dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Make sure the UserData within is for ourselves, since we called the [UpdateOwnGlobalPerm]
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Don't modify others perms when calling updateOwnPerm").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // fetch the user global perm from the database.
        UserGlobalPermissions? perms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // Attempt to make the change.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Failed to set property to new Value!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);
        }

        // Otherwise, we correctly set the property and updated things.
        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        Dictionary<string, string> pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

        // callback to the client caller's pairs, letting them know that our permission was updated.
        await Clients.Users(pairsOfClient.Select(p => p.Key)).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        // callback to the client caller to let them know that their permissions have been updated.
        await Clients.Caller.Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<HubResponse> UserChangeOwnPairPerm(SingleChangeUnique dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // grab the pair permission, where the user is the client caller, and the other user is the one we are updating the pair permissions for.
        ClientPairPermissions? perms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // store to see if change is a pause change
        bool prevPauseState = perms.IsPaused;

        // Attempt to make the change.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Failed to set property to new Value!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);
        }

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // callback the updated info to the client caller as well so it can update properly.
        await Clients.User(UserUID).Callback_SingleChangeUnique(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        // send a callback to the userpair we updated our permission for, so they get the updated info
        await Clients.User(dto.User.UID).Callback_SingleChangeUnique(new(new(UserUID), dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);

        // check pause change
        if (!(perms.IsPaused != prevPauseState))
            return HubResponseBuilder.Yippee();

        // we have performed a pause change, so need to make sure that we send online/offline respectively base on update.
        _logger.LogMessage("Pause change detected, checking if both users are online to send online/offline updates.");
        // grab the other players pair perms for you
        ClientPairPermissions? otherPairData = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (otherPairData is not null && !otherPairData.IsPaused)
        {
            // only perform the following if they are online.
            string otherCharaIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
            if (UserCharaIdent is null || otherCharaIdent is null)
                return HubResponseBuilder.Yippee();

            // if the new value is true (we are pausing them) and they have not paused us, we must send offline for both.
            if ((bool)dto.NewPerm.Value)
            {
                await Clients.User(UserUID).Callback_KinksterOffline(new(new(dto.User.UID))).ConfigureAwait(false);
                await Clients.User(dto.User.UID).Callback_KinksterOffline(new(new(UserUID))).ConfigureAwait(false);
            }
            // Otherwise, its false, and they dont have us paused, so send online to both.
            else
            {
                await Clients.User(UserUID).Callback_KinksterOnline(new(new(dto.User.UID), otherCharaIdent)).ConfigureAwait(false);
                await Clients.User(dto.User.UID).Callback_KinksterOnline(new(new(UserUID), UserCharaIdent)).ConfigureAwait(false);
            }
        }

        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<HubResponse> UserChangeOwnPairPermAccess(SingleChangeAccess dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // grab the edit access permission
        ClientPairPermissionAccess? perms = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
        if (perms is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Pair Access permission not found").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // Attempt to make the change to the permissions.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Failed to set property to new Value!").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);
        }

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send a callback to the userpair we updated our permission for, so they get the updated info (we update the user so that when the pair receives it they know who to update this for)
        await Clients.User(dto.User.UID).Callback_SingleChangeAccess(new(new(UserUID), dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        // callback the updated info to the client caller as well so it can update properly.
        await Clients.Caller.Callback_SingleChangeAccess(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> 
    ///     Method will remove all associated things with the user and delete their
    ///     profile from the server, along with all other profiles under their account.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserDelete()
    {
        _logger.LogCallInfo();

        // fetch the client callers user data from the database.
        User userEntry = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // for any other profiles registered under this account, fetch them from the database as well.
        List<User?> secondaryUsers = await DbContext.Auth.Include(u => u.User)
            .Where(u => u.PrimaryUserUID == UserUID)
            .Select(c => c.User)
            .ToListAsync().ConfigureAwait(false);

        // remove all the client callers secondary profiles, then finally, remove their primary profile. (dont through helper functions)
        foreach (User? user in secondaryUsers)
        {
            if (user is not null) 
                await DeleteUser(user).ConfigureAwait(false);
        }
        await DeleteUser(userEntry).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }
}

