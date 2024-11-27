using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0051 // Method is too long
/// <summary> 
/// This partial class of the GagSpeakHub contains all the user related methods for pushing data.
/// </summary>
public partial class GagspeakHub
{
    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character COMBINED data
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
        await Clients.Users(recipientUids).Client_UserReceiveDataComposite(new(new(UserUID), dto.CompositeData)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataComposite);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataCompositeTo, recipientUids.Count);
    }

    /// <summary> Called by a connected client to push own latest IPC Data to other paired clients. </summary>
    public async Task UserPushDataIpc(UserCharaIpcDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        // fetch the new Dto to send (with Client Caller's userData as the attached User) to other paired clients.
        await Clients.Users(recipientUids).Client_UserReceiveDataIpc(new(new(UserUID), new(UserUID), dto.IpcData, dto.Type)).ConfigureAwait(false);

        // update metrics.
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's APPEARANCE Data
    /// </summary>
    public async Task UserPushDataAppearance(UserCharaAppearanceDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // Grab our Appearance from the DB
        var curGagData = await DbContext.UserAppearanceData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (curGagData == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot update other Pair, User does not have appearance data!").ConfigureAwait(false);
            return;
        }

        switch (dto.Type)
        {
            case GagUpdateType.GagApplied:
                curGagData.UpdateGagState(dto.UpdatedLayer, dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].GagType.ToGagType());
                break;

            case GagUpdateType.GagLocked:
                var slotData = dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer];
                curGagData.GagLockUpdate(dto.UpdatedLayer, slotData.Padlock.ToPadlock(), slotData.Password, slotData.Assigner, slotData.Timer);
                break;

            case GagUpdateType.GagUnlocked:
                curGagData.GagUnlockUpdate(dto.UpdatedLayer);
                break;

            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case GagUpdateType.GagRemoved:
                curGagData.UpdateGagState(dto.UpdatedLayer, GagType.None);
                break;

            case GagUpdateType.Safeword:
                // clear the appearance data for all gags.
                foreach (GagLayer layer in Enum.GetValues(typeof(GagLayer)))
                {
                    curGagData.UpdateGagState(layer, GagType.None);
                    curGagData.GagUnlockUpdate(layer);
                }
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.Type);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(curGagData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newAppearance = curGagData.ToApiAppearanceData();

        await Clients.Users(recipientUids).Client_UserReceiveDataAppearance(new(new(UserUID), new(UserUID), newAppearance, dto.UpdatedLayer, dto.Type, dto.PreviousLock)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAppearance(new(new(UserUID), new(UserUID), newAppearance, dto.UpdatedLayer, dto.Type, dto.PreviousLock)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearance);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearanceTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's WARDROBE Data
    /// </summary>
    public async Task UserPushDataWardrobe(UserCharaWardrobeDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (userActiveState == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "You somehow does not have active state data!").ConfigureAwait(false);
            return;
        }

        switch (dto.Type)
        {
            case WardrobeUpdateType.FullDataUpdate:
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                userActiveState.ActiveSetPadLock = dto.WardrobeData.Padlock;
                userActiveState.ActiveSetPassword = dto.WardrobeData.Password;
                userActiveState.ActiveSetLockTime = dto.WardrobeData.Timer;
                userActiveState.ActiveSetLockAssigner = dto.WardrobeData.Assigner;
                break;

            case WardrobeUpdateType.RestraintApplied:
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                break;

            case WardrobeUpdateType.RestraintLocked:
                userActiveState.RestraintLockUpdate(
                    dto.WardrobeData.Padlock.ToPadlock(), 
                    dto.WardrobeData.Password, 
                    dto.WardrobeData.Assigner, 
                    dto.WardrobeData.Timer);
                break;

            case WardrobeUpdateType.RestraintUnlocked:
                userActiveState.RestraintUnlockUpdate();
                break;

            case WardrobeUpdateType.RestraintDisabled:
                userActiveState.ActiveSetId = Guid.Empty;
                userActiveState.ActiveSetEnabler = string.Empty;
                break;

            case WardrobeUpdateType.CursedItemApplied:
            case WardrobeUpdateType.CursedItemRemoved:
                break;

            case WardrobeUpdateType.Safeword:
                userActiveState.ActiveSetId = Guid.Empty;
                userActiveState.ActiveSetEnabler = string.Empty;
                userActiveState.RestraintUnlockUpdate();
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Wardrobe Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Wardrobe Data: " + dto.Type);
                return;
        }

        // update the database with the new active state data.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.Users(recipientUids).Client_UserReceiveDataWardrobe(new(new(UserUID), new(UserUID), newWardrobeData, dto.Type, dto.PreviousLock)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataWardrobe(new(new(UserUID), new(UserUID), newWardrobeData, dto.Type, dto.PreviousLock)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobe);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobeTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ALIAS Data
    /// </summary>
    public async Task UserPushDataAlias(UserCharaAliasDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // convert the recipient UID list from the recipient list of the dto
        var recipientUidList = new List<string>() { dto.RecipientUser.UID };
        var recipientUid = dto.RecipientUser.UID;

        // if the recipient is not cached, cache them.
        bool isCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUidList, Context.ConnectionAborted).ConfigureAwait(false);
        if (!isCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUidList = allPairedUsers.Where(f => recipientUidList.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        await Clients.User(recipientUid).Client_UserReceiveDataAlias(new(new(UserUID), new(UserUID), dto.AliasData, dto.Type)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.RecipientUser, new(UserUID), dto.AliasData, dto.Type)).ConfigureAwait(false); // don't see why we need it, remove if excess overhead in the end.

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAlias);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAliasTo, 2);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ToyboxData 
    /// </summary>
    public async Task UserPushDataToybox(UserCharaToyboxDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        await Clients.Users(recipientUids).Client_UserReceiveDataToybox(new(new(UserUID), new(UserUID), dto.ToyboxInfo, dto.Type)).ConfigureAwait(false);
        // could also send back data to caller if need be, but no real reason for that at the moment ? (maybe could remove it from others too? Idk
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ToyboxData 
    /// </summary>
    public async Task UserPushDataLightStorage(UserCharaLightStorageMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        await Clients.Users(recipientUids).Client_UserReceiveLightStorage(new(new(UserUID), new(UserUID), dto.LightStorage)).ConfigureAwait(false);
        // could also send back data to caller if need be, but no real reason for that at the moment ? (maybe could remove it from others too? Idk
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, recipientUids.Count);
    }

    /// <summary>
    /// Bumps the change in Appearance Data to another pair.
    /// </summary>
    public async Task UserPushPairDataAppearanceUpdate(OnlineUserCharaAppearanceDataDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update self, only use this to update another pair!").ConfigureAwait(false);
            return;
        }

        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?").ConfigureAwait(false);
            return;
        }
        else if (!pairPermissions.GagFeatures) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "GagFeatures not modifiable for Pair!").ConfigureAwait(false);
            return;
        }

        var currentAppearanceData = await DbContext.UserAppearanceData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (currentAppearanceData == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update other Pair, User does not have appearance data!").ConfigureAwait(false);
            return;
        }

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // Filter the list to only include online pairs
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        // Convert from Dictionary<string,string> to List<string> of UID's.
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        switch (dto.Type)
        {
            case GagUpdateType.GagApplied:
                if (!DataUpdateHelpers.CanApplyGag(currentAppearanceData, dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Slot One is already Locked & cant be replaced occupied!").ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.UpdateGagState(dto.UpdatedLayer, dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].GagType.ToGagType());
                break;

            case GagUpdateType.GagLocked:
                if (!DataUpdateHelpers.CanLockGag(currentAppearanceData, pairPermissions, dto.UpdatedLayer, out string ErrorMsg)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, ErrorMsg).ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.GagLockUpdate(dto.UpdatedLayer,
                    dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].Padlock.ToPadlock(),
                    dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].Password,
                    dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].Assigner,
                    dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer].Timer);
                break;

            case GagUpdateType.GagUnlocked:
                if (!DataUpdateHelpers.CanUnlockGag(currentAppearanceData, pairPermissions, dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer], dto.UpdatedLayer, out string unlockError)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, unlockError).ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.GagUnlockUpdate(dto.UpdatedLayer);
                break;

            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case GagUpdateType.GagRemoved:
                if (currentAppearanceData.CanRemoveGag(dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "This Slot cannot be removed, it's currently locked!").ConfigureAwait(false);
                }
                currentAppearanceData.UpdateGagState(dto.UpdatedLayer, GagType.None);
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.Type);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(currentAppearanceData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newAppearanceData = currentAppearanceData.ToApiAppearanceData();

        await Clients.User(dto.User.UID).Client_UserReceiveDataAppearance(new(new(UserUID), dto.Enactor, newAppearanceData, dto.UpdatedLayer, dto.Type, dto.PreviousPadlock)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataAppearance(new(dto.User, dto.Enactor, newAppearanceData, dto.UpdatedLayer, dto.Type, dto.PreviousPadlock)).ConfigureAwait(false);

        // Inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearance);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearanceTo, allOnlinePairsOfAffectedPairUids.Count);
    }

    public async Task UserPushPairDataWardrobeUpdate(OnlineUserCharaWardrobeDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Fetch affected pair's current activeState data from the DB
        var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (userActiveState == null) throw new Exception("User has no Active State Data!");

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // Filter the list to only include online pairs
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        // Convert from Dictionary<string,string> to List<string> of UID's.
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        switch (dto.Type)
        {
            case WardrobeUpdateType.RestraintApplied:
                if (!pairPermissions.ApplyRestraintSets) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeApplying on them!").ConfigureAwait(false);
                    return;
                }
                if (!userActiveState.ActiveSetId.IsEmptyGuid() && userActiveState.ActiveSetPadLock.ToPadlock() is not Padlocks.None) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Replace Currently Active Set because it is currently locked!").ConfigureAwait(false);
                    return;
                }
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                break;

            case WardrobeUpdateType.RestraintLocked:
                if (!DataUpdateHelpers.CanLockRestraint(userActiveState, pairPermissions, dto.WardrobeData, out string lockError))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, lockError).ConfigureAwait(false);
                    return;
                }

                userActiveState.RestraintLockUpdate(
                    dto.WardrobeData.Padlock.ToPadlock(),
                    dto.WardrobeData.Password,
                    dto.WardrobeData.Assigner,
                    dto.WardrobeData.Timer);
                break;

            case WardrobeUpdateType.RestraintUnlocked:
                if (!DataUpdateHelpers.CanUnlockRestraint(userActiveState, pairPermissions, dto.WardrobeData, out string unlockError))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, unlockError).ConfigureAwait(false);
                    return;
                }
                userActiveState.RestraintUnlockUpdate();
                break;

            case WardrobeUpdateType.RestraintDisabled:
                if (userActiveState.ActiveSetId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No active set to remove!").ConfigureAwait(false);
                    return;
                }
                if (userActiveState.ActiveSetPadLock.ToPadlock() is not Padlocks.None) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Active set is still locked!").ConfigureAwait(false);
                    return;
                }
                userActiveState.ActiveSetId = Guid.Empty;
                break;

            case WardrobeUpdateType.CursedItemApplied:
            case WardrobeUpdateType.CursedItemRemoved:
                break;

            default:
                throw new Exception("Invalid UpdateKind for Wardrobe Data!");
        }

        // update the changes to the database.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var updatedWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.User(dto.User.UID).Client_UserReceiveDataWardrobe(new(new(UserUID), dto.Enactor, updatedWardrobeData, dto.Type, dto.PreviousLock)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataWardrobe(new(dto.User, dto.Enactor, updatedWardrobeData, dto.Type, dto.PreviousLock)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobe);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobeTo, allOnlinePairsOfAffectedPairUids.Count);
    }

    public async Task UserPushPairDataAliasStorageUpdate(OnlineUserCharaAliasDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // verify that a pair between the two clients is made.
        var pairPermissions = await DbContext.ClientPairs.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // ensure that the update kind of to change the registered names. If it is not, throw exception.
        if (dto.Type is not PuppeteerUpdateType.PlayerNameRegistered) throw new Exception("Invalid UpdateKind for Pair Alias Data!");

        // in our dto, we have the PAIR WE ARE PROVIDING OUR NAME TO as the user-data, with our name info inside.
        // so when we construct the message to update the client's OWN data, we need to place the client callers name info inside.
        await Clients.User(dto.User.UID).Client_UserReceiveDataAlias(new(new(UserUID), dto.Enactor, dto.AliasData, dto.Type)).ConfigureAwait(false);

        // when we push the update back to our client caller, we must inform them that the client callers name was updated.
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.User, dto.Enactor, dto.AliasData, dto.Type)).ConfigureAwait(false);
    }

    public async Task UserPushPairDataToyboxUpdate(OnlineUserCharaToyboxDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        switch (dto.Type)
        {
            case ToyboxUpdateType.PatternExecuted:
                if (!pairPermissions.CanExecutePatterns || dto.ToyboxInfo.InteractionId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to execute Patterns, or you used a Guid.Empty transaction ID!").ConfigureAwait(false);
                    return;
                }
                break;
            case ToyboxUpdateType.PatternStopped:
                if (!pairPermissions.CanExecutePatterns || dto.ToyboxInfo.InteractionId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to stop Patterns, or you used a Guid.Empty transaction ID!").ConfigureAwait(false);
                    return;
                }
                break;

            case ToyboxUpdateType.AlarmToggled:
                if (!pairPermissions.CanToggleAlarms || dto.ToyboxInfo.InteractionId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to toggle Alarms, or you used a Guid.Empty transaction ID!").ConfigureAwait(false);
                    return;
                }
                break;

            case ToyboxUpdateType.TriggerToggled:
                if (!pairPermissions.CanToggleTriggers || dto.ToyboxInfo.InteractionId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to toggle triggers, or you used a Guid.Empty transaction ID!").ConfigureAwait(false);
                    return;
                }
                break;
            default:
                throw new Exception("Invalid UpdateKind for Toybox Data!");
        }

        await Clients.User(dto.User.UID).Client_UserReceiveDataToybox(new(new(UserUID), dto.Enactor, dto.ToyboxInfo, dto.Type)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataToybox(new(dto.User, dto.Enactor, dto.ToyboxInfo, dto.Type)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, allOnlinePairsOfAffectedPairUids.Count);
    }
}
#pragma warning restore MA0051 // Method is too long
