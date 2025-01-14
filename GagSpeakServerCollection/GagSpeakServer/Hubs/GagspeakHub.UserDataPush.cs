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
        await Clients.Users(recipientUids).Client_UserReceiveDataIpc(new(new(UserUID), new(UserUID), dto.IpcData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
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

        var updateSlot = dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer];

        // we can always assume that this is correct when applied by self.
        switch (dto.Type)
        {
            case GagUpdateType.GagApplied:
                curGagData.NewGagType(dto.UpdatedLayer, updateSlot.GagType);
                break;

            case GagUpdateType.GagLocked:
                curGagData.NewPadlock(dto.UpdatedLayer, updateSlot.Padlock);
                curGagData.NewPassword(dto.UpdatedLayer, updateSlot.Password);
                curGagData.NewTimer(dto.UpdatedLayer, updateSlot.Timer);
                curGagData.NewAssigner(dto.UpdatedLayer, updateSlot.Assigner);
                break;

            case GagUpdateType.GagUnlocked:
                curGagData.NewPadlock(dto.UpdatedLayer, updateSlot.Padlock);
                curGagData.NewPassword(dto.UpdatedLayer, updateSlot.Password);
                curGagData.NewTimer(dto.UpdatedLayer, updateSlot.Timer);
                curGagData.NewAssigner(dto.UpdatedLayer, updateSlot.Assigner);
                break;

            case GagUpdateType.GagRemoved:
                curGagData.NewGagType(dto.UpdatedLayer, updateSlot.GagType);
                break;

            case GagUpdateType.MimicGagApplied:
                curGagData.NewGagType(dto.UpdatedLayer, updateSlot.GagType);
                curGagData.NewPadlock(dto.UpdatedLayer, updateSlot.Padlock);
                curGagData.NewPassword(dto.UpdatedLayer, updateSlot.Password);
                curGagData.NewTimer(dto.UpdatedLayer, updateSlot.Timer);
                curGagData.NewAssigner(dto.UpdatedLayer, updateSlot.Assigner);
                break;

            case GagUpdateType.Safeword:
                // clear the appearance data for all gags.
                foreach (GagLayer layer in Enum.GetValues(typeof(GagLayer)))
                {
                    curGagData.NewGagType(layer, GagType.None.GagName());
                    curGagData.NewPadlock(layer, Padlocks.None.ToName());
                    curGagData.NewPassword(layer, string.Empty);
                    curGagData.NewTimer(layer, DateTimeOffset.UtcNow);
                    curGagData.NewAssigner(layer, string.Empty);
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

        await Clients.Users(recipientUids).Client_UserReceiveDataAppearance(new(new(UserUID), new(UserUID), newAppearance, dto.UpdatedLayer, dto.Type, dto.PreviousLock, UpdateDir.Other)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAppearance(new(new(UserUID), new(UserUID), newAppearance, dto.UpdatedLayer, dto.Type, dto.PreviousLock, UpdateDir.Own)).ConfigureAwait(false);
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

        var userActiveState = await DbContext.UserActiveSetData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (userActiveState == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "You somehow does not have active state data!").ConfigureAwait(false);
            return;
        }

        switch (dto.Type)
        {
            case WardrobeUpdateType.FullDataUpdate:
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                userActiveState.Padlock = dto.WardrobeData.Padlock;
                userActiveState.Password = dto.WardrobeData.Password;
                userActiveState.Timer = dto.WardrobeData.Timer;
                userActiveState.Assigner = dto.WardrobeData.Assigner;
                break;

            case WardrobeUpdateType.RestraintApplied:
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                break;

            case WardrobeUpdateType.RestraintLocked:
                userActiveState.Padlock = dto.WardrobeData.Padlock;
                userActiveState.Password = dto.WardrobeData.Password;
                userActiveState.Timer = dto.WardrobeData.Timer;
                userActiveState.Assigner = dto.WardrobeData.Assigner;
                break;

            case WardrobeUpdateType.RestraintUnlocked:
                userActiveState.Padlock = Padlocks.None.ToName();
                userActiveState.Password = string.Empty;
                userActiveState.Timer = DateTimeOffset.UtcNow;
                userActiveState.Assigner = string.Empty;
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
                userActiveState.Padlock = Padlocks.None.ToName();
                userActiveState.Password = string.Empty;
                userActiveState.Timer = DateTimeOffset.UtcNow;
                userActiveState.Assigner = string.Empty;
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Wardrobe Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Wardrobe Data: " + dto.Type);
                return;
        }

        // update the database with the new active state data.
        DbContext.UserActiveSetData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // accounts for any possible tampered client side shinanagin bullshit.
        var newWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.Users(recipientUids).Client_UserReceiveDataWardrobe(new(new(UserUID), new(UserUID), newWardrobeData, dto.Type, dto.PreviousLock, UpdateDir.Other)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataWardrobe(new(new(UserUID), new(UserUID), newWardrobeData, dto.Type, dto.PreviousLock, UpdateDir.Own)).ConfigureAwait(false);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ORDERS Data
    /// </summary>
    public async Task UserPushDataOrders(UserCharaOrdersDataMessageDto dto)
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

        // currently does nothing.
        switch (dto.Type)
        {
            case OrdersUpdateType.FullDataUpdate:
                break;

            case OrdersUpdateType.OrderAssigned:
                break;

            case OrdersUpdateType.OrderProgressMade:
                break;

            case OrdersUpdateType.OrderCompleted:
                break;

            case OrdersUpdateType.OrderDisabled:
                break;

            case OrdersUpdateType.Safeword:
                break;
        }

        await Clients.Users(recipientUids).Client_UserReceiveDataOrders(new(new(UserUID), new(UserUID), dto.OrdersData, dto.Type, dto.AffectedItem, UpdateDir.Other)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataOrders(new(new(UserUID), new(UserUID), dto.OrdersData, dto.Type, dto.AffectedItem, UpdateDir.Own)).ConfigureAwait(false);
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

        await Clients.User(recipientUid).Client_UserReceiveDataAlias(new(new(UserUID), new(UserUID), dto.AliasData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.RecipientUser, new(UserUID), dto.AliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false); // don't see why we need it, remove if excess overhead in the end.
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

        await Clients.Users(recipientUids).Client_UserReceiveDataToybox(new(new(UserUID), new(UserUID), dto.ToyboxInfo, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
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

        await Clients.Users(recipientUids).Client_UserReceiveLightStorage(new(new(UserUID), new(UserUID), dto.LightStorage, UpdateDir.Other)).ConfigureAwait(false);
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
        if (!allCached) await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        var dtoSlotData = dto.AppearanceData.GagSlots[(int)dto.UpdatedLayer];

        switch (dto.Type)
        {
            case GagUpdateType.GagApplied:
                if (!pairPermissions.ApplyGags || !currentAppearanceData.CanApplyOrLockGag(dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Apply Gags!").ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.NewGagType(dto.UpdatedLayer, dtoSlotData.GagType);
                break;

            case GagUpdateType.GagLocked:
                if (!pairPermissions.LockGags || !currentAppearanceData.CanApplyOrLockGag(dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Lock Gags!").ConfigureAwait(false);
                    return;
                }
                // validate if we can lock the gag, if not, throw a warning.
                var lockCode = dto.AppearanceData.IsLockUpdateValid(dto.UpdatedLayer, pairPermissions);
                if (lockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Lock:"+ lockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the lock.
                currentAppearanceData.NewPadlock(dto.UpdatedLayer, dtoSlotData.Padlock);
                currentAppearanceData.NewPassword(dto.UpdatedLayer, dtoSlotData.Password);
                currentAppearanceData.NewTimer(dto.UpdatedLayer, dtoSlotData.Timer);
                currentAppearanceData.NewAssigner(dto.UpdatedLayer, dtoSlotData.Assigner);
                break;

            case GagUpdateType.GagUnlocked:
                if (!pairPermissions.UnlockGags || !currentAppearanceData.CanRemoveGag(dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Unlock Gags!").ConfigureAwait(false);
                    return;
                }
                // validate if we can unlock the gag, if not, throw a warning.
                var unlockCode = currentAppearanceData.IsUnlockUpdateValid(UserUID, dto.UpdatedLayer, dtoSlotData, pairPermissions);
                if (unlockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Unlock:"+ unlockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the unlock.
                currentAppearanceData.NewPadlock(dto.UpdatedLayer, dtoSlotData.Padlock);
                currentAppearanceData.NewPassword(dto.UpdatedLayer, dtoSlotData.Password);
                currentAppearanceData.NewTimer(dto.UpdatedLayer, dtoSlotData.Timer);
                currentAppearanceData.NewAssigner(dto.UpdatedLayer, dtoSlotData.Assigner);
                break;

            case GagUpdateType.GagRemoved:
                if (!pairPermissions.RemoveGags || currentAppearanceData.CanRemoveGag(dto.UpdatedLayer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Remove Gags!").ConfigureAwait(false);
                    return;
                }
                currentAppearanceData.NewGagType(dto.UpdatedLayer, dtoSlotData.GagType);
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

        await Clients.User(dto.User.UID).Client_UserReceiveDataAppearance(new(new(UserUID), dto.Enactor, newAppearanceData, dto.UpdatedLayer, dto.Type, dto.PreviousPadlock, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataAppearance(new(dto.User, dto.Enactor, newAppearanceData, dto.UpdatedLayer, dto.Type, dto.PreviousPadlock, UpdateDir.Other)).ConfigureAwait(false);
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
        var userActiveState = await DbContext.UserActiveSetData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
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
                if (!pairPermissions.ApplyRestraintSets || !userActiveState.ActiveSetId.IsEmptyGuid() && userActiveState.Padlock.ToPadlock() is not Padlocks.None) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Replace Currently Active Set because it is currently locked!").ConfigureAwait(false);
                    return;
                }
                userActiveState.ActiveSetId = dto.WardrobeData.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.WardrobeData.ActiveSetEnabledBy;
                break;

            case WardrobeUpdateType.RestraintLocked:
                // see if we are able to lock the restraint, and if we are, do so, otherwise, return the error code.
                var lockResultCode = dto.WardrobeData.IsLockUpdateValid(pairPermissions);
                if (lockResultCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Lock: " + lockResultCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the stuff.
                userActiveState.Padlock = dto.WardrobeData.Padlock;
                userActiveState.Password = dto.WardrobeData.Password;
                userActiveState.Timer = dto.WardrobeData.Timer;
                userActiveState.Assigner = dto.WardrobeData.Assigner;
                break;

            case WardrobeUpdateType.RestraintUnlocked:
                // see if we are able to unlock the restraint, and if we are, do so, otherwise, return the error code.
                var unlockResultCode = userActiveState.IsUnlockUpdateValid(UserUID, dto.WardrobeData, pairPermissions);
                if (unlockResultCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Unlock: " + unlockResultCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the stuff.
                userActiveState.Padlock = dto.WardrobeData.Padlock;
                userActiveState.Password = dto.WardrobeData.Password;
                userActiveState.Timer = dto.WardrobeData.Timer;
                userActiveState.Assigner = dto.WardrobeData.Assigner;
                break;

            case WardrobeUpdateType.RestraintDisabled:
                if (userActiveState.ActiveSetId.IsEmptyGuid()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No active set to remove!").ConfigureAwait(false);
                    return;
                }
                if (userActiveState.Padlock.ToPadlock() is not Padlocks.None) {
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
        DbContext.UserActiveSetData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var updatedWardrobeData = DataUpdateHelpers.BuildUpdatedWardrobeData(dto.WardrobeData, userActiveState);

        await Clients.User(dto.User.UID).Client_UserReceiveDataWardrobe(new(new(UserUID), dto.Enactor, updatedWardrobeData, dto.Type, dto.PreviousLock, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataWardrobe(new(dto.User, dto.Enactor, updatedWardrobeData, dto.Type, dto.PreviousLock, UpdateDir.Other)).ConfigureAwait(false);
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
        await Clients.User(dto.User.UID).Client_UserReceiveDataAlias(new(new(UserUID), dto.Enactor, dto.AliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);

        // when we push the update back to our client caller, we must inform them that the client callers name was updated.
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.User, dto.Enactor, dto.AliasData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
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

        await Clients.User(dto.User.UID).Client_UserReceiveDataToybox(new(new(UserUID), dto.Enactor, dto.ToyboxInfo, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataToybox(new(dto.User, dto.Enactor, dto.ToyboxInfo, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
    }
}
#pragma warning restore MA0051 // Method is too long
