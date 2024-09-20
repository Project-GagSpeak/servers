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

/// <summary> 
/// This partial class of the GagSpeakHub contains all the user related methods for pushing data.
/// </summary>
public partial class GagspeakHub
{
    private const string None = "None";
    private const string OwnerPadlock = "OwnerPadlock";
    private const string OwnerTimerPadlock = "OwnerTimerPadlock";


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
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataComposite(
            new OnlineUserCompositeDataDto(new UserData(UserUID), dto.CompositeData, DataUpdateKind.FullDataUpdate)).ConfigureAwait(false);

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
        await Clients.Users(recipientUids).Client_UserReceiveOtherDataIpc(new OnlineUserCharaIpcDataDto(new UserData(UserUID), dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);

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
        if (curGagData == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot update other Pair, User does not have appearance data!").ConfigureAwait(false);
            return;
        }

        NewState requestedState = dto.UpdateKind.ToNewState();
        GagLayer requestLayer = dto.UpdateKind.ToSlot();

        switch (dto.UpdateKind)
        {
            case DataUpdateKind.AppearanceGagAppliedLayerOne:
            case DataUpdateKind.AppearanceGagAppliedLayerTwo:
            case DataUpdateKind.AppearanceGagAppliedLayerThree:
                curGagData.UpdateGagState(requestLayer, dto.AppearanceData.GagSlots[(int)requestLayer].GagType.ToGagType());
                break;

            case DataUpdateKind.AppearanceGagLockedLayerOne:
            case DataUpdateKind.AppearanceGagLockedLayerTwo:
            case DataUpdateKind.AppearanceGagLockedLayerThree:
                var slotData = dto.AppearanceData.GagSlots[(int)requestLayer];
                curGagData.UpdateGagLockState(requestLayer, slotData.Padlock.ToPadlock(), slotData.Password, slotData.Assigner, slotData.Timer);
                break;

            case DataUpdateKind.AppearanceGagUnlockedLayerOne:
            case DataUpdateKind.AppearanceGagUnlockedLayerTwo:
            case DataUpdateKind.AppearanceGagUnlockedLayerThree:
                curGagData.UpdateGagLockState(requestLayer, Padlocks.None, string.Empty, string.Empty, DateTimeOffset.UtcNow);
                break;

            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case DataUpdateKind.AppearanceGagRemovedLayerOne:
            case DataUpdateKind.AppearanceGagRemovedLayerTwo:
            case DataUpdateKind.AppearanceGagRemovedLayerThree:
                curGagData.UpdateGagState(requestLayer, GagType.None);
                curGagData.UpdateGagLockState(requestLayer, Padlocks.None, string.Empty, string.Empty, DateTimeOffset.UtcNow);
                break;

            case DataUpdateKind.Safeword:
                // clear the appearance data for all gags.
                foreach (GagLayer layer in Enum.GetValues(typeof(GagLayer)))
                {
                    curGagData.UpdateGagState(layer, GagType.None);
                    curGagData.UpdateGagLockState(layer, Padlocks.None, string.Empty, string.Empty, DateTimeOffset.UtcNow, true);
                }
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.UpdateKind).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.UpdateKind);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(curGagData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newAppearance = curGagData.ToApiAppearanceData();

        await Clients.Users(recipientUids).Client_UserReceiveOtherDataAppearance(new(new(UserUID), dto.AppearanceData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveOwnDataAppearance(new(new(UserUID), dto.AppearanceData, dto.UpdateKind)).ConfigureAwait(false);

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
        if (userActiveState == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "You somehow does not have active state data!").ConfigureAwait(false);
            return;
        }

        switch (dto.UpdateKind)
        {
            case DataUpdateKind.WardrobeRestraintOutfitsUpdated:
                break;

            case DataUpdateKind.WardrobeRestraintApplied:
                userActiveState.WardrobeActiveSetName = dto.WardrobeData.ActiveSetName;
                break;

            case DataUpdateKind.WardrobeRestraintLocked:
                userActiveState.UpdateWardrobeSetLock(
                    dto.WardrobeData.Padlock.ToPadlock(), 
                    dto.WardrobeData.Password, 
                    dto.WardrobeData.Assigner, 
                    dto.WardrobeData.Timer);
                break;

            case DataUpdateKind.WardrobeRestraintUnlocked:
                userActiveState.UpdateWardrobeSetLock(
                    dto.WardrobeData.Padlock.ToPadlock(),
                    string.Empty, 
                    string.Empty, 
                    DateTimeOffset.UtcNow, 
                    true);
                break;

            case DataUpdateKind.WardrobeRestraintDisabled:
                userActiveState.WardrobeActiveSetName = string.Empty;
                break;
            case DataUpdateKind.Safeword:
                userActiveState.WardrobeActiveSetName = string.Empty;
                userActiveState.UpdateWardrobeSetLock(dto.WardrobeData.Padlock.ToPadlock(), string.Empty, string.Empty, DateTimeOffset.UtcNow, true);
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Wardrobe Data: " + dto.UpdateKind).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Wardrobe Data: " + dto.UpdateKind);
                return;
        }

        // update the database with the new active state data.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.Users(recipientUids).Client_UserReceiveOtherDataWardrobe(new(new(UserUID), newWardrobeData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveOwnDataWardrobe(new(new(UserUID), newWardrobeData, dto.UpdateKind)).ConfigureAwait(false);

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

        await Clients.User(recipientUid).Client_UserReceiveOtherDataAlias(new(new(UserUID), dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveOwnDataAlias(new(dto.RecipientUser, dto.AliasData, dto.UpdateKind)).ConfigureAwait(false); // don't see why we need it, remove if excess overhead in the end.

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

        // REVIEW: We can input checks against active state data here if we run into concurrency issues.
        if (dto.UpdateKind == DataUpdateKind.Safeword)
        {
            var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (userActiveState == null)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update own userActiveStateData!").ConfigureAwait(false);
                return;
            }
            userActiveState.ToyboxActivePatternId = Guid.Empty;
            // update the database with the new appearance data.
            DbContext.UserActiveStateData.Update(userActiveState);
        }

        await Clients.Users(recipientUids).Client_UserReceiveOtherDataToybox(new(new(UserUID), dto.PatternInfo, dto.UpdateKind)).ConfigureAwait(false);
        // could also send back data to caller if need be, but no real reason for that at the moment ? (maybe could remove it from others too? Idk
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, recipientUids.Count);
    }

    public async Task UserPushPiShockUpdate(UserCharaPiShockPermMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (dto.UpdateKind != DataUpdateKind.PiShockGlobalUpdated && dto.UpdateKind != DataUpdateKind.PiShockOwnPermsForPairUpdated) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Invalid UpdateKind for PiShock Permissions Data!").ConfigureAwait(false);
            return;
        }

        if (dto.UpdateKind is DataUpdateKind.PiShockGlobalUpdated)
        {
            // get the recipient UID list from the recipient list of the dto
            var recipientUidList = dto.Recipients.Select(r => r.UID).ToList();
            bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUidList, Context.ConnectionAborted).ConfigureAwait(false);
            if (!allCached)
            {
                var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
                recipientUidList = allPairedUsers.Where(f => recipientUidList.Contains(f, StringComparer.Ordinal)).ToList();
                await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
            }

            if (dto.UpdateKind == DataUpdateKind.Safeword)
            {
                var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
                if (userActiveState == null) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update own userActiveStateData!").ConfigureAwait(false);
                    return;
                }
                userActiveState.ToyboxActivePatternId = Guid.Empty;
                DbContext.UserActiveStateData.Update(userActiveState);
            }

            await Clients.Users(recipientUidList).Client_UserReceiveDataPiShock(new(new(UserUID), dto.ShockPermissions, dto.UpdateKind)).ConfigureAwait(false);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShock);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShockTo, recipientUidList.Count);
        }
        else if (dto.UpdateKind is DataUpdateKind.PiShockOwnPermsForPairUpdated or DataUpdateKind.PiShockPairPermsForUserUpdated)
        {
            // it is a push to update a spesific pair. So we should ensure the Recipients list only contains 1 element.
            if (dto.Recipients.Count != 1) {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Should not be notifying multiple users when updating permissions for one!").ConfigureAwait(false);
                return;
            }

            // only search for that user and cache in online synced service.
            var recipientUid = dto.Recipients[0].UID;
            bool isCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, new List<string>() { recipientUid }, Context.ConnectionAborted).ConfigureAwait(false);
            if (!isCached)
            {
                var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
                await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
            }

            await Clients.User(recipientUid).Client_UserReceiveDataPiShock(new(new(UserUID), dto.ShockPermissions, DataUpdateKind.PiShockPairPermsForUserUpdated)).ConfigureAwait(false);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShock);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShockTo, 1);
        }
    }


    /// <summary>
    /// Bumps the change in Moodles Data to another pair.
    /// </summary>
    public async Task UserPushPairDataIpcUpdate(OnlineUserCharaIpcDataDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update self, only use this to update another pair!").ConfigureAwait(false);
            return;
        }

        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?").ConfigureAwait(false);
            return;
        }

        // Because the person changing the pairs permission doesnt know all of the pair's added UserPairs, fetch them.
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // grab the subset of them that are online.
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all online pairs of pair are cached. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataIpc(new(new(UserUID), dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataIpc(new(dto.User, dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo, allOnlinePairsOfAffectedPairUids.Count);
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

        // get the data summarized.
        NewState requestedState = dto.UpdateKind.ToNewState();
        GagLayer requestLayer = dto.UpdateKind.ToSlot();

        switch (dto.UpdateKind)
        {
            case DataUpdateKind.AppearanceGagAppliedLayerOne:
            case DataUpdateKind.AppearanceGagAppliedLayerTwo:
            case DataUpdateKind.AppearanceGagAppliedLayerThree:
                if (!DataUpdateHelpers.CanApplyGag(currentAppearanceData, requestLayer))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Slot One is already Locked & cant be replaced occupied!").ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.UpdateGagState(requestLayer, dto.AppearanceData.GagSlots[(int)requestLayer].GagType.ToGagType());
                break;

            case DataUpdateKind.AppearanceGagLockedLayerOne:
            case DataUpdateKind.AppearanceGagLockedLayerTwo:
            case DataUpdateKind.AppearanceGagLockedLayerThree:
                if (!DataUpdateHelpers.CanLockGag(currentAppearanceData, pairPermissions, requestLayer, out string ErrorMsg))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, ErrorMsg).ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.UpdateGagLockState(requestLayer,
                    dto.AppearanceData.GagSlots[(int)requestLayer].Padlock.ToPadlock(),
                    dto.AppearanceData.GagSlots[(int)requestLayer].Password,
                    dto.AppearanceData.GagSlots[(int)requestLayer].Assigner,
                    dto.AppearanceData.GagSlots[(int)requestLayer].Timer);
                break;

            case DataUpdateKind.AppearanceGagUnlockedLayerOne:
            case DataUpdateKind.AppearanceGagUnlockedLayerTwo:
            case DataUpdateKind.AppearanceGagUnlockedLayerThree:
                if (!DataUpdateHelpers.CanUnlockGag(currentAppearanceData, pairPermissions, dto.AppearanceData.GagSlots[(int)requestLayer], requestLayer, out string unlockError))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, unlockError).ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.UpdateGagLockState(requestLayer, 
                    dto.AppearanceData.GagSlots[(int)requestLayer].Padlock.ToPadlock(), 
                    string.Empty,
                    string.Empty,
                    DateTimeOffset.UtcNow,
                    true);
                break;

            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case DataUpdateKind.AppearanceGagRemovedLayerOne:
            case DataUpdateKind.AppearanceGagRemovedLayerTwo:
            case DataUpdateKind.AppearanceGagRemovedLayerThree:
                if (currentAppearanceData.CanRemoveGag(requestLayer))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "This Slot cannot be removed, it's currently locked!").ConfigureAwait(false);
                }
                currentAppearanceData.UpdateGagState(requestLayer, GagType.None);
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.UpdateKind).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.UpdateKind);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(currentAppearanceData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newAppearanceData = currentAppearanceData.ToApiAppearanceData();

        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataAppearance(new(new(UserUID), newAppearanceData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataAppearance(new(new(dto.User.UID), newAppearanceData, dto.UpdateKind)).ConfigureAwait(false);

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

        switch (dto.UpdateKind)
        {
            case DataUpdateKind.WardrobeRestraintApplied:
                if (!pairPermissions.ApplyRestraintSets)
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeApplying on them!").ConfigureAwait(false);
                    return;
                }
                if (!string.IsNullOrEmpty(userActiveState.WardrobeActiveSetName) && userActiveState.WardrobeActiveSetPadLock.ToPadlock() is not Padlocks.None)
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Replace Currently Active Set because it is currently locked!").ConfigureAwait(false);
                    return;
                }
                userActiveState.WardrobeActiveSetName = dto.WardrobeData.ActiveSetName;
                break;

            case DataUpdateKind.WardrobeRestraintLocked:
                if (!DataUpdateHelpers.CanLockRestraint(userActiveState, pairPermissions, dto.WardrobeData, out string lockError))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, lockError).ConfigureAwait(false);
                    return;
                }
                userActiveState.UpdateWardrobeSetLock(
                    dto.WardrobeData.Padlock.ToPadlock(), 
                    dto.WardrobeData.Password, 
                    dto.WardrobeData.Assigner, 
                    dto.WardrobeData.Timer);
                break;

            case DataUpdateKind.WardrobeRestraintUnlocked:
                if (!DataUpdateHelpers.CanUnlockRestraint(userActiveState, pairPermissions, dto.WardrobeData, out string unlockError))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, unlockError).ConfigureAwait(false);
                    return;
                }
                userActiveState.UpdateWardrobeSetLock(
                    dto.WardrobeData.Padlock.ToPadlock(),
                    string.Empty, 
                    string.Empty, 
                    DateTimeOffset.UtcNow,
                    true);
                break;

            case DataUpdateKind.WardrobeRestraintDisabled:
                if (string.IsNullOrEmpty(userActiveState.WardrobeActiveSetName))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No active set to remove!").ConfigureAwait(false);
                    return;
                }
                if (!string.Equals(userActiveState.WardrobeActiveSetPadLock, None, StringComparison.Ordinal))
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Active set is still locked!").ConfigureAwait(false);
                    return;
                }
                userActiveState.WardrobeActiveSetName = string.Empty;
                break;

            default:
                throw new Exception("Invalid UpdateKind for Wardrobe Data!");
        }

        // update the changes to the database.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // build new DTO to send off.
        var updatedWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataWardrobe(new(new(UserUID), updatedWardrobeData, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataWardrobe(new(dto.User, updatedWardrobeData, dto.UpdateKind)).ConfigureAwait(false);

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
        if (dto.UpdateKind != DataUpdateKind.PuppeteerPlayerNameRegistered) throw new Exception("Invalid UpdateKind for Pair Alias Data!");

        // in our dto, we have the PAIR WE ARE PROVIDING OUR NAME TO as the user-data, with our name info inside.
        // so when we construct the message to update the client's OWN data, we need to place the client callers name info inside.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataAlias(new(new UserData(UserUID), dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);

        // when we push the update back to our client caller, we must inform them that the client callers name was updated.
        await Clients.Caller.Client_UserReceiveOtherDataAlias(new(dto.User, dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);
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

        // Perform security check on the UpdateKind, to make sure client caller is not exploiting the system.
        switch (dto.UpdateKind)
        {
            case DataUpdateKind.ToyboxPatternExecuted:
                if (!pairPermissions.CanExecutePatterns) throw new Exception("Pair doesn't allow you to use ToyboxPatternFeatures on them!");
                // ensure that we have a pattern set to active that should be set to active.
                var activePattern = dto.ToyboxInfo.ActivePatternGuid;
                if (activePattern == Guid.Empty) throw new Exception("Cannot activate Guid.Empty!");

                // otherwise, we should activate that pattern in our user state.
                userActiveState.ToyboxActivePatternId = activePattern;
                break;
            case DataUpdateKind.ToyboxPatternStopped:
                if (!pairPermissions.CanExecutePatterns) throw new Exception("Pair doesn't allow you to use ToyboxPatternFeatures on them!");
                // Throw if no pattern was playing.
                if (userActiveState.ToyboxActivePatternId == Guid.Empty) throw new Exception("No active pattern was playing, nothing to stop!");
                // If we reach here, the passed in data is valid (all patterns should no longer be active) and we can send back the data.
                userActiveState.ToyboxActivePatternId = Guid.Empty;
                break;
            case DataUpdateKind.ToyboxAlarmListUpdated: throw new Exception("Cannot modify this type of data here!");
            case DataUpdateKind.ToyboxAlarmToggled:
                if (!pairPermissions.CanToggleAlarms) throw new Exception("Pair doesn't allow you to use ToyboxAlarmFeatures on them!");
                break;
            case DataUpdateKind.ToyboxTriggerListUpdated: throw new Exception("Cannot modify this type of data here!");
            case DataUpdateKind.ToyboxTriggerToggled:
                if (!pairPermissions.CanToggleTriggers) throw new Exception("Pair doesn't allow you to use ToyboxTriggerFeatures on them!");
                break;
            default:
                throw new Exception("Invalid UpdateKind for Toybox Data!");
        }

        // update the changes to the database.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // nothing changes here directly with the userActiveState, so just return original for now...

        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataToybox(new(new(UserUID), dto.ToyboxInfo, dto.UpdateKind)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataToybox(new(dto.User, dto.ToyboxInfo, dto.UpdateKind)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, allOnlinePairsOfAffectedPairUids.Count);
    }
}