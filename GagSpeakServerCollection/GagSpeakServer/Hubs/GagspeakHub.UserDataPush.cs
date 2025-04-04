using GagspeakAPI;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0051 // Method is too long

/// <summary> This partial class of the GagSpeakHub contains all the user related methods for pushing data. </summary>
public partial class GagspeakHub
{
    /// <summary> For pushing all data when going online or after using a safeword. This is the most heavy call, and should be used sparingly. </summary>
    [Authorize(Policy = "Identified")]
    public async Task<GsApiErrorCodes> UserPushData(PushCompositeDataMessageDto dto)
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
            if (curGagData.Contains(null))
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot clear Gag Data, it does not exist!").ConfigureAwait(false);
                return GsApiErrorCodes.GagRelated | GsApiErrorCodes.NullEntry | GsApiErrorCodes.DeleteFailed;
            }

            // Grab the restriction data ordered by layer.
            List<UserRestrictionData> curRestrictionData = await DbContext.UserRestrictionData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            if (curRestrictionData.Contains(null))
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot clear Active Restriction Data, it does not exist!").ConfigureAwait(false);
                return GsApiErrorCodes.RestrictionRelated | GsApiErrorCodes.NullEntry | GsApiErrorCodes.DeleteFailed;
            }

            // Grab the restraint set data.
            UserRestraintData curRestraintData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curRestraintData is null)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot clear Restraint Data, it does not exist!").ConfigureAwait(false);
                return GsApiErrorCodes.RestraintRelated | GsApiErrorCodes.NullEntry | GsApiErrorCodes.DeleteFailed;
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
            await Clients.Users(recipientUids).Client_UserReceiveDataComposite(new(new(UserUID), dto.CompositeData, dto.WasSafeword)).ConfigureAwait(false);
        }
        else
        {
            // Push the composite data off to the other pairs.
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
            await Clients.Users(recipientUids).Client_UserReceiveDataComposite(new(new(UserUID), dto.CompositeData, dto.WasSafeword)).ConfigureAwait(false);
        }

        return GsApiErrorCodes.Success;
    }

    /// <summary> Called by a connected client to push own latest IPC Data to other paired clients. </summary>
    public async Task UserPushDataIpc(PushIpcDataUpdateDto dto)
    {
        List<string> recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        // Do not handle Caching for IPC Data, as caching is detrimental when the calls occur frequently.
        await Clients.Users(recipientUids).Client_UserReceiveDataIpc(new(new(UserUID), new(UserUID), dto.IpcData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
    }

    public async Task<GsApiErrorCodes> UserPushDataGags(PushGagDataUpdateDto dto)
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
        UserGagData curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curGagData is null)
            return GsApiErrorCodes.GagRelated | GsApiErrorCodes.NullEntry | GsApiErrorCodes.InvalidLayer;

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
                return GsApiErrorCodes.GagRelated | GsApiErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated appearance.
        GagspeakAPI.Data.Character.ActiveGagSlot newAppearance = curGagData.ToApiGagSlot();
        CallbackGagDataDto recipientDto = new CallbackGagDataDto(new(UserUID), new(UserUID), newAppearance, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        await Clients.Users(recipientUids).Client_UserReceiveDataGags(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataGags(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);

        return GsApiErrorCodes.Success;
    }

    /// <summary> Client Pushing their own restriction Data. </summary>
    public async Task<GsApiErrorCodes> UserPushDataRestrictions(PushRestrictionDataUpdateDto dto)
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

        UserRestrictionData curRestrictionData = await DbContext.UserRestrictionData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curRestrictionData is null)
            return GsApiErrorCodes.RestrictionRelated | GsApiErrorCodes.NullEntry | GsApiErrorCodes.InvalidLayer;

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
                return GsApiErrorCodes.RestrictionRelated | GsApiErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restrictionData.
        GagspeakAPI.Data.Character.ActiveRestriction newRestrictionData = curRestrictionData.ToApiRestrictionSlot();
        CallbackRestrictionDataDto recipientDto = new CallbackRestrictionDataDto(new(UserUID), new(UserUID), newRestrictionData, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevId,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Client_UserReceiveDataRestrictions(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataRestrictions(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);

        return GsApiErrorCodes.Success;
    }

    public async Task<GsApiErrorCodes> UserPushDataRestraint(PushRestraintDataUpdateDto dto)
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
        UserRestraintData curRestraintData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (curRestraintData is null)
            return GsApiErrorCodes.RestraintRelated | GsApiErrorCodes.NullEntry;

        Guid prevSetId = curRestraintData.Identifier;
        byte prevLayers = curRestraintData.LayersBitfield;
        Padlocks prevPadlock = curRestraintData.Padlock;

        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curRestraintData.Identifier = dto.ActiveSetId;
                curRestraintData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.LayerToggled:
                curRestraintData.LayersBitfield = dto.LayersBitfield;
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
                return GsApiErrorCodes.RestraintRelated | GsApiErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // compile to api and push out.
        GagspeakAPI.Data.Character.CharaActiveRestraint newRestraintData = curRestraintData.ToApiRestraintData();
        CallbackRestraintDataDto recipientDto = new CallbackRestraintDataDto(new(UserUID), new(UserUID), newRestraintData, dto.Type, UpdateDir.Other)
        {
            PreviousRestraint = prevSetId,
            PreviousLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Client_UserReceiveDataRestraint(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataRestraint(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);

        return GsApiErrorCodes.Success;
    }

    public async Task<GsApiErrorCodes> UserPushDataCursedLoot(PushCursedLootDataUpdateDto dto)
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
        if(dto.InteractedLoot.GagItem is not GagType.None)
        {
            // Grab the user gag data associated with our name, and find the first layer that does not have a gag item. If none found, return error.
            UserGagData curGagData = await DbContext.UserGagData
                .FirstOrDefaultAsync(data => data.UserUID == UserUID && data.Gag == GagType.None)
                .ConfigureAwait(false);
            if (curGagData is null)
                return GsApiErrorCodes.CursedLootRelated | GsApiErrorCodes.GagRelated | GsApiErrorCodes.NoAvailableLayer;

            // get the previous gag.
            GagType previousGag = curGagData.Gag;

            // update the data.
            curGagData.Gag = dto.InteractedLoot.GagItem;
            curGagData.Enabler = "Mimic";
            curGagData.Padlock = Padlocks.MimicPadlock;
            curGagData.Password = string.Empty;
            curGagData.Timer = dto.InteractedLoot.ReleaseTime;
            curGagData.PadlockAssigner = "Mimic";

            // save changes to our tracked item.
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            GagspeakAPI.Data.Character.ActiveGagSlot newAppearance = curGagData.ToApiGagSlot();
            CallbackGagDataDto recipientDto = new CallbackGagDataDto(new(UserUID), new(UserUID), newAppearance, DataUpdateType.AppliedCursed, UpdateDir.Other)
            {
                AffectedLayer = curGagData.Layer,
                PreviousGag = previousGag,
                PreviousPadlock = Padlocks.None
            };
            // Return Gag Update.
            await Clients.Users(recipientUids).Client_UserReceiveDataGags(recipientDto).ConfigureAwait(false);
            await Clients.Caller.Client_UserReceiveDataGags(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        }

        // Push CursedLoot update to all recipients.
        await Clients.Users(recipientUids).Client_UserReceiveDataCursedLoot(new(new(UserUID), dto.ActiveItems) { InteractedLoot = dto.InteractedLoot }).ConfigureAwait(false);
        return GsApiErrorCodes.Success;
    }


    /// <summary> Client Pushing their own Orders Data. </summary>
    public async Task<GsApiErrorCodes> UserPushDataOrders(PushOrdersDataUpdateDto dto)
    {
        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Orders are not yet implemented.").ConfigureAwait(false);
        return GsApiErrorCodes.OrdersRelated;
    }

    /// <summary> Client Pushing their own Alias Data. </summary>
    public async Task<GsApiErrorCodes> UserPushDataAlias(PushAliasDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // convert the recipient UID list from the recipient list of the dto
        await Clients.User(dto.RecipientUser.UID).Client_UserReceiveDataAlias(new(new(UserUID), new(UserUID), dto.AliasData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.RecipientUser, new(UserUID), dto.AliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        return GsApiErrorCodes.Success;
    }

    /// <summary> Client Pushing their own Toybox Data. </summary>
    public async Task<GsApiErrorCodes> UserPushDataToybox(PushToyboxDataUpdateDto dto)
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
                if (dto.LatestActiveItems.ActiveAlarms.Contains(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveAlarms.Remove(dto.AffectedIdentifier);
                else
                    dto.LatestActiveItems.ActiveAlarms.Add(dto.AffectedIdentifier);
                break;

            case DataUpdateType.TriggerToggled:
                if (dto.LatestActiveItems.ActiveTriggers.Contains(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveTriggers.Remove(dto.AffectedIdentifier);
                else
                    dto.LatestActiveItems.ActiveTriggers.Add(dto.AffectedIdentifier);
                break;

            default:
                return GsApiErrorCodes.ToyboxRelated | GsApiErrorCodes.BadDataUpdateKind;
        }

        await Clients.Users(recipientUids).Client_UserReceiveDataToybox(new(new(UserUID), new(UserUID), dto.LatestActiveItems, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        return GsApiErrorCodes.Success;
    }

    /// <summary> Client pushing its light storage data. </summary>
    public async Task<GsApiErrorCodes> UserPushDataLightStorage(PushLightStorageMessageDto dto)
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

        await Clients.Users(recipientUids).Client_UserReceiveLightStorage(new(new(UserUID), new(UserUID), dto.LightStorage, UpdateDir.Other)).ConfigureAwait(false);
        return GsApiErrorCodes.Success;
    }

    /// <summary> Changes another pairs gag data, if allowance permits. </summary>
    public async Task<GsApiPairErrorCodes> UserPushPairDataGags(PushPairGagDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return GsApiPairErrorCodes.AttemptedSelfChange;

        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return GsApiPairErrorCodes.NotPaired;

        UserGagData currentGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (currentGagData is null)
            return GsApiPairErrorCodes.InvalidLayer;

        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        if (!await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false))
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        GagType previousGag = currentGagData.Gag;
        Padlocks previousPadlock = currentGagData.Padlock;

        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyGags)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (currentGagData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                currentGagData.Gag = dto.Gag;
                currentGagData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                if (currentGagData.Gag is GagType.None) 
                    return GsApiPairErrorCodes.NoActiveItem;

                if (currentGagData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                if (!pairPerms.LockGags)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.PermanentDenied;

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.OwnerDenied;

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.DevotionalDenied;

                // do a final validation pass.
                GsApiPairErrorCodes finalLockPass = dto.CanLock(currentGagData, pairPerms.MaxGagTime);
                if (finalLockPass is not GsApiPairErrorCodes.Success)
                    return finalLockPass;

                currentGagData.Padlock = dto.Padlock;
                currentGagData.Password = dto.Password;
                currentGagData.Timer = dto.Timer;
                currentGagData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (currentGagData.Gag is GagType.None)
                    return GsApiPairErrorCodes.NoActiveItem;

                else if (!currentGagData.IsLocked())
                    return GsApiPairErrorCodes.NotCurrentlyLocked;

                else if (!pairPerms.UnlockGags)
                    return GsApiPairErrorCodes.PermissionDenied;

                // validate if we can unlock the gag, if not, throw a warning.
                GsApiPairErrorCodes finalUnlockPass = dto.CanUnlock(dto.Recipient.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GsApiPairErrorCodes.Success)
                    return finalUnlockPass;

                currentGagData.Padlock = Padlocks.None;
                currentGagData.Password = string.Empty;
                currentGagData.Timer = DateTimeOffset.MinValue;
                currentGagData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                if (currentGagData.Gag is GagType.None)
                    return GsApiPairErrorCodes.NoActiveItem;
                
                if (currentGagData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;
                
                if (!pairPerms.RemoveGags)
                    return GsApiPairErrorCodes.PermissionDenied;
                
                currentGagData.Gag = GagType.None;
                currentGagData.Enabler = string.Empty;
                break;

            default:
                return GsApiPairErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        GagspeakAPI.Data.Character.ActiveGagSlot newGagData = currentGagData.ToApiGagSlot();
        CallbackGagDataDto recipientDto = new CallbackGagDataDto(new(dto.User.UID), new(UserUID), newGagData, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        // send to recipient.
        await Clients.User(dto.User.UID).Client_UserReceiveDataGags(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        // send back to all recipients pairs. (including client caller)
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataGags(recipientDto with { Direction = UpdateDir.Other }).ConfigureAwait(false);
        return GsApiPairErrorCodes.Success;
    }

    /// <summary> Changes another pairs restriction data, if allowance permits. </summary>
    public async Task<GsApiPairErrorCodes> UserPushPairDataRestrictions(PushPairRestrictionDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return GsApiPairErrorCodes.AttemptedSelfChange;

        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return GsApiPairErrorCodes.NotPaired;

        UserRestrictionData curRestrictionData = await DbContext.UserRestrictionData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curRestrictionData is null)
            return GsApiPairErrorCodes.InvalidLayer;

        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        if (!await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false))
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        Guid prevRestriction = curRestrictionData.Identifier;
        Padlocks prevPadlock = curRestrictionData.Padlock;
        // Extra validation checks made on the server for security reasons.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyRestrictions)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (curRestrictionData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                curRestrictionData.Identifier = dto.RestrictionId;
                curRestrictionData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                if (curRestrictionData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                if (!pairPerms.LockRestrictions)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.PermanentDenied;

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.OwnerDenied;

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.DevotionalDenied;

                // do a final validation pass.
                GsApiPairErrorCodes finalLockPass = dto.CanLock(curRestrictionData, pairPerms.MaxRestrictionTime);
                if (finalLockPass is not GsApiPairErrorCodes.Success)
                    return finalLockPass;

                curRestrictionData.Padlock = dto.Padlock;
                curRestrictionData.Password = dto.Password;
                curRestrictionData.Timer = dto.Timer;
                curRestrictionData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                else if (!curRestrictionData.IsLocked())
                    return GsApiPairErrorCodes.NotCurrentlyLocked;

                else if (!pairPerms.UnlockRestrictions)
                    return GsApiPairErrorCodes.PermissionDenied;

                GsApiPairErrorCodes finalUnlockPass = dto.CanUnlock(dto.Recipient.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GsApiPairErrorCodes.Success)
                    return finalUnlockPass;

                curRestrictionData.Padlock = Padlocks.None;
                curRestrictionData.Password = string.Empty;
                curRestrictionData.Timer = DateTimeOffset.MinValue;
                curRestrictionData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                if (curRestrictionData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                if (curRestrictionData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                if (!pairPerms.RemoveRestrictions)
                    return GsApiPairErrorCodes.PermissionDenied;

                curRestrictionData.Identifier = Guid.Empty;
                curRestrictionData.Enabler = string.Empty;
                break;

            default:
                return GsApiPairErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        GagspeakAPI.Data.Character.ActiveRestriction newRestrictionData = curRestrictionData.ToApiRestrictionSlot();
        CallbackRestrictionDataDto recipientDto = new CallbackRestrictionDataDto(new(dto.User.UID), new(UserUID), newRestrictionData, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevRestriction,
            PreviousPadlock = prevPadlock
        };

        await Clients.User(dto.User.UID).Client_UserReceiveDataRestrictions(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataRestrictions(recipientDto with { Direction = UpdateDir.Other }).ConfigureAwait(false);
        return GsApiPairErrorCodes.Success;
    }

    public async Task<GsApiPairErrorCodes> UserPushPairDataRestraint(PushPairRestraintDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return GsApiPairErrorCodes.AttemptedSelfChange;

        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return GsApiPairErrorCodes.NullEntry | GsApiPairErrorCodes.NotPaired;

        UserRestraintData curRestraintSetData = await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (curRestraintSetData is null)
            return GsApiPairErrorCodes.NullEntry | GsApiPairErrorCodes.NoEntryFound;

        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        Guid prevRestraintSet = curRestraintSetData.Identifier;
        byte prevBitField = curRestraintSetData.LayersBitfield;
        Padlocks prevPadlock = curRestraintSetData.Padlock;
        // Extra validation checks made on the server for security reasons.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                if (!pairPerms.ApplyRestraintSets)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (curRestraintSetData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                curRestraintSetData.Identifier = dto.ActiveSetId;
                curRestraintSetData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.LayerToggled:
                if (!pairPerms.ApplyRestraintLayers)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                curRestraintSetData.LayersBitfield = dto.LayersBitfield;
                break;

            case DataUpdateType.Locked:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                if (curRestraintSetData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                if (!pairPerms.LockRestraintSets)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (!pairPerms.PermanentLocks && dto.Padlock.IsPermanentLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.PermanentDenied;

                if (!pairPerms.OwnerLocks && dto.Padlock.IsOwnerLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.OwnerDenied;

                if (!pairPerms.DevotionalLocks && dto.Padlock.IsDevotionalLock())
                    return GsApiPairErrorCodes.PermissionDenied | GsApiPairErrorCodes.DevotionalDenied;

                // do a final validation pass.
                GsApiPairErrorCodes finalLockPass = dto.CanLock(curRestraintSetData, pairPerms.MaxRestrictionTime);
                if (finalLockPass is not GsApiPairErrorCodes.Success)
                    return finalLockPass;

                curRestraintSetData.Padlock = dto.Padlock;
                curRestraintSetData.Password = dto.Password;
                curRestraintSetData.Timer = dto.Timer;
                curRestraintSetData.PadlockAssigner = dto.PadlockAssigner;
                break;

            case DataUpdateType.Unlocked:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                else if (!curRestraintSetData.IsLocked())
                    return GsApiPairErrorCodes.NotCurrentlyLocked;

                else if (!pairPerms.UnlockRestraintSets)
                    return GsApiPairErrorCodes.PermissionDenied;

                GsApiPairErrorCodes finalUnlockPass = dto.CanUnlock(dto.Recipient.UID, dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (finalUnlockPass is not GsApiPairErrorCodes.Success)
                    return finalUnlockPass;

                curRestraintSetData.Padlock = Padlocks.None;
                curRestraintSetData.Password = string.Empty;
                curRestraintSetData.Timer = DateTimeOffset.MinValue;
                curRestraintSetData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                if (curRestraintSetData.Identifier.IsEmptyGuid())
                    return GsApiPairErrorCodes.NoActiveItem;

                if (curRestraintSetData.IsLocked())
                    return GsApiPairErrorCodes.AlreadyLocked;

                if (!pairPerms.RemoveRestraintSets)
                    return GsApiPairErrorCodes.PermissionDenied;

                curRestraintSetData.Identifier = Guid.Empty;
                curRestraintSetData.Enabler = string.Empty;
                break;

            default:
                return GsApiPairErrorCodes.BadDataUpdateKind;
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        GagspeakAPI.Data.Character.CharaActiveRestraint updatedWardrobeData = curRestraintSetData.ToApiRestraintData();
        CallbackRestraintDataDto recipientDto = new CallbackRestraintDataDto(dto.Recipient, new(UserUID), updatedWardrobeData, dto.Type, UpdateDir.Other)
        {
            PreviousRestraint = prevRestraintSet,
            PreviousLayers = prevBitField,
            PreviousPadlock = prevPadlock
        };

        await Clients.User(dto.User.UID).Client_UserReceiveDataRestraint(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataRestraint(recipientDto).ConfigureAwait(false);
        return GsApiPairErrorCodes.Success;
    }

    public async Task<GsApiPairErrorCodes> UserPushPairDataAliasStorage(PushPairAliasDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return GsApiPairErrorCodes.AttemptedSelfChange;

        // verify that a pair between the two clients is made.
        ClientPair pairPerms = await DbContext.ClientPairs.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return GsApiPairErrorCodes.NotPaired;

        if (dto.Type is not DataUpdateType.NameRegistered)
            return GsApiPairErrorCodes.BadDataUpdateKind;

        // in our dto, we have the PAIR WE ARE PROVIDING OUR NAME TO as the user-data, with our name info inside.
        // so when we construct the message to update the client's OWN data, we need to place the client callers name info inside.
        await Clients.User(dto.User.UID).Client_UserReceiveDataAlias(new(new(dto.User.UID), new(UserUID), dto.LastAliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAlias(new(new(dto.User.UID), new(UserUID), dto.LastAliasData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        return GsApiPairErrorCodes.Success;
    }

    public async Task<GsApiPairErrorCodes> UserPushPairDataToybox(PushPairToyboxDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return GsApiPairErrorCodes.AttemptedSelfChange;

        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms is null)
            return GsApiPairErrorCodes.NullEntry | GsApiPairErrorCodes.NotPaired;

        // Grabs all Pairs of the affected pair
        List<string> allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        Dictionary<string, string> allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        List<string> allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);
        // validate change over server.
        switch (dto.Type)
        {
            case DataUpdateType.PatternSwitched:
            case DataUpdateType.PatternExecuted:
                if (!pairPerms.ExecutePatterns)
                    return GsApiPairErrorCodes.PermissionDenied;

                dto.LastToyboxData.ActivePattern = dto.AffectedIdentifier;
                break;

            case DataUpdateType.PatternStopped:
                if (!pairPerms.StopPatterns)
                    return GsApiPairErrorCodes.PermissionDenied;

                dto.LastToyboxData.ActivePattern = Guid.Empty;
                break;

            case DataUpdateType.AlarmToggled:
                if (!pairPerms.ToggleAlarms)
                    return GsApiPairErrorCodes.PermissionDenied;
                
                if (dto.LastToyboxData.ActiveAlarms.Contains(dto.AffectedIdentifier))
                    dto.LastToyboxData.ActiveAlarms.Remove(dto.AffectedIdentifier);
                else
                    dto.LastToyboxData.ActiveAlarms.Add(dto.AffectedIdentifier);
                break;

            case DataUpdateType.TriggerToggled:
                if (!pairPerms.ToggleTriggers)
                    return GsApiPairErrorCodes.PermissionDenied;

                if (dto.LastToyboxData.ActiveTriggers.Contains(dto.AffectedIdentifier))
                    dto.LastToyboxData.ActiveTriggers.Remove(dto.AffectedIdentifier);
                else
                    dto.LastToyboxData.ActiveTriggers.Add(dto.AffectedIdentifier);
                break;

            default:
                return GsApiPairErrorCodes.BadDataUpdateKind;
        }

        await Clients.User(dto.User.UID).Client_UserReceiveDataToybox(new(dto.Recipient, new(UserUID), dto.LastToyboxData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataToybox(new(dto.Recipient, new(UserUID), dto.LastToyboxData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        return GsApiPairErrorCodes.Success;
    }
}
#pragma warning restore MA0051 // Method is too long
