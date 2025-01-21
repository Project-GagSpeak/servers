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
    public async Task UserPushData(PushCompositeDataMessageDto dto)
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

        // if a safeword, we need to clear all the data for the appearance and activeSetData.
        if (dto.WasSafeword)
        {
            var curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curGagData == null)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot clear Gag Data, it does not exist!").ConfigureAwait(false);
                return;
            }

            var curActiveSetData = await DbContext.UserActiveSetData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curActiveSetData == null)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot clear Active Restraint Data, it does not exist!").ConfigureAwait(false);
                return;
            }

            // clear the appearance data for all gags.
            foreach (GagLayer layer in Enum.GetValues(typeof(GagLayer)))
            {
                curGagData.NewGagType(layer, GagType.None.GagName());
                curGagData.NewPadlock(layer, Padlocks.None.ToName());
                curGagData.NewPassword(layer, string.Empty);
                curGagData.NewTimer(layer, DateTimeOffset.UtcNow);
                curGagData.NewAssigner(layer, string.Empty);
            }

            // clear the active set data.
            curActiveSetData.ActiveSetId = Guid.Empty;
            curActiveSetData.ActiveSetEnabler = string.Empty;
            curActiveSetData.Padlock = Padlocks.None.ToName();
            curActiveSetData.Password = string.Empty;
            curActiveSetData.Timer = DateTimeOffset.UtcNow;
            curActiveSetData.Assigner = string.Empty;

            // update the database with the new appearance data.
            DbContext.UserGagData.Update(curGagData);
            DbContext.UserActiveSetData.Update(curActiveSetData);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // this SHOULD be fine to update after a safeword as we would have triggered all functionality related to these changes on the client side beforehand.
        }


        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveDataComposite(new(new(UserUID), dto.CompositeData, dto.WasSafeword)).ConfigureAwait(false);
    }

    /// <summary> Called by a connected client to push own latest IPC Data to other paired clients. </summary>
    public async Task UserPushDataIpc(PushIpcDataUpdateDto dto)
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

    public async Task UserPushDataGags(PushGagDataUpdateDto dto)
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
        var curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (curGagData == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Cannot update other Pair, User does not have appearance data!").ConfigureAwait(false);
            return;
        }

        // get the previous gag type and padlock from the current data.
        var previousGag = curGagData.GetGagType(dto.Layer);
        var previousPadlock = curGagData.GetGagPadlock(dto.Layer);

        // we can always assume that this is correct when applied by self.
        switch (dto.Type)
        {
            case GagUpdateType.Applied:
                curGagData.NewGagType(dto.Layer, dto.Gag.GagName());
                break;

            case GagUpdateType.Locked:
                curGagData.NewPadlock(dto.Layer, dto.Padlock.ToName());
                curGagData.NewPassword(dto.Layer, dto.Password);
                curGagData.NewTimer(dto.Layer, dto.Timer);
                curGagData.NewAssigner(dto.Layer, dto.Assigner);
                break;

            case GagUpdateType.Unlocked:
                curGagData.NewPadlock(dto.Layer, dto.Padlock.ToName());
                curGagData.NewPassword(dto.Layer, dto.Password);
                curGagData.NewTimer(dto.Layer, dto.Timer);
                curGagData.NewAssigner(dto.Layer, dto.Assigner);
                break;

            case GagUpdateType.Removed:
                curGagData.NewGagType(dto.Layer, dto.Gag.GagName());
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.Type);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserGagData.Update(curGagData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newAppearance = curGagData.ToApiGagData();

        var recipientDto = new OnlineUserGagDataDto(new(UserUID), new(UserUID), newAppearance, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        await Clients.Users(recipientUids).Client_UserReceiveDataAppearance(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAppearance(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's WARDROBE Data
    /// </summary>
    public async Task UserPushDataRestraint(PushRestraintDataUpdateDto dto)
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

        var prevSetId = userActiveState.ActiveSetId;
        var prevPadlock = userActiveState.Padlock.ToPadlock();

        switch (dto.Type)
        {
            case WardrobeUpdateType.Applied:
                userActiveState.ActiveSetId = dto.ActiveSetId;
                userActiveState.ActiveSetEnabler = dto.Enabler;
                break;

            case WardrobeUpdateType.Locked:
                userActiveState.Padlock = dto.Padlock.ToName();
                userActiveState.Password = dto.Password;
                userActiveState.Timer = dto.Timer;
                userActiveState.Assigner = dto.Assigner;
                break;

            case WardrobeUpdateType.Unlocked:
                userActiveState.Padlock = Padlocks.None.ToName();
                userActiveState.Password = string.Empty;
                userActiveState.Timer = DateTimeOffset.UtcNow;
                userActiveState.Assigner = string.Empty;
                break;

            case WardrobeUpdateType.Disabled:
                userActiveState.ActiveSetId = Guid.Empty;
                userActiveState.ActiveSetEnabler = string.Empty;
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Wardrobe Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Wardrobe Data: " + dto.Type);
                return;
        }

        // update the database with the new active state data.
        DbContext.UserActiveSetData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // compile to api and push out.
        var newWardrobeData = userActiveState.ToApiActiveSetData();

        var recipientDto = new OnlineUserRestraintDataDto(new(UserUID), new(UserUID), newWardrobeData, dto.Type, UpdateDir.Other)
        {
            RestraintModified = dto.ActiveSetId,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Client_UserReceiveDataWardrobe(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataWardrobe(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
    }

    public async Task<bool> UserPushDataCursedLoot(PushCursedLootDataUpdateDto dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // Handle the cursed loot based on the type.
        if(dto.IsCursedGag())
        {
            // Grab our Appearance from the DB
            var curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curGagData is null)
                return false;

            // get the previous gag.
            var previousGag = curGagData.GetGagType(dto.Layer);

            // update the gag data from the respective layer.
            curGagData.NewGagType(dto.Layer, dto.GagType.GagName());
            curGagData.NewPadlock(dto.Layer, Padlocks.MimicPadlock.ToName());
            curGagData.NewPassword(dto.Layer, string.Empty);
            curGagData.NewTimer(dto.Layer, dto.ReleaseTime);
            curGagData.NewAssigner(dto.Layer, UserUID);

            // update the database with the new appearance data and push the changes out.
            DbContext.UserGagData.Update(curGagData);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var newAppearance = curGagData.ToApiGagData();

            var recipientDto = new OnlineUserGagDataDto(new(UserUID), new(UserUID), newAppearance, GagUpdateType.AppliedCursed, UpdateDir.Other)
            {
                AffectedLayer = dto.Layer,
                PreviousGag = previousGag,
                PreviousPadlock = Padlocks.None // should always be none anyways so should not madder.
            };

            await Clients.Users(recipientUids).Client_UserReceiveDataAppearance(recipientDto).ConfigureAwait(false);
            await Clients.Caller.Client_UserReceiveDataAppearance(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        }

        // Send back out that cursedLootData is updated for our user.
        await Clients.Users(recipientUids).Client_UserReceiveDataCursedLoot(new(new(UserUID), dto.ActiveItems) { LastInteractedItem = dto.LootIdInteracted }).ConfigureAwait(false);
        // return true to us to let us know the operation was successful.
        return true;
    }


    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ORDERS Data
    /// </summary>
    public async Task UserPushDataOrders(PushOrdersDataUpdateDto dto)
    {
        // Do not do anything right now with this.
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ALIAS Data
    /// </summary>
    public async Task UserPushDataAlias(PushAliasDataUpdateDto dto)
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
        await Clients.Caller.Client_UserReceiveDataAlias(new(dto.RecipientUser, new(UserUID), dto.AliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ToyboxData 
    /// </summary>
    public async Task UserPushDataToybox(PushToyboxDataUpdateDto dto)
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

        // we need to update our lists based on the interacted item, and store what item was interacted with.
        switch (dto.Type)
        {
            case ToyboxUpdateType.PatternExecuted:
                dto.LatestActiveItems.ActivePattern = dto.AffectedIdentifier;
                break;

            case ToyboxUpdateType.PatternStopped:
                dto.LatestActiveItems.ActivePattern = Guid.Empty;
                break;

            case ToyboxUpdateType.AlarmToggled:
                if (dto.LatestActiveItems.ActiveAlarms.Contains(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveAlarms.Remove(dto.AffectedIdentifier);
                else
                    dto.LatestActiveItems.ActiveAlarms.Add(dto.AffectedIdentifier);
                break;

            case ToyboxUpdateType.TriggerToggled:
                if (dto.LatestActiveItems.ActiveTriggers.Contains(dto.AffectedIdentifier))
                    dto.LatestActiveItems.ActiveTriggers.Remove(dto.AffectedIdentifier);
                else
                    dto.LatestActiveItems.ActiveTriggers.Add(dto.AffectedIdentifier);
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Toybox Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Toybox Data: " + dto.Type);
                return;
        }

        await Clients.Users(recipientUids).Client_UserReceiveDataToybox(new(new(UserUID), new(UserUID), dto.LatestActiveItems, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
        // can add own later if need be!
    }

    public async Task UserPushDataLightStorage(PushLightStorageMessageDto dto)
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
    public async Task UserPushPairDataGags(PushPairGagDataUpdateDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update self, only use this to update another pair!").ConfigureAwait(false);
            return;
        }

        var pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?").ConfigureAwait(false);
            return;
        }

        var currentGagData = await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (currentGagData == null) {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update other Pair, User does not have appearance data!").ConfigureAwait(false);
            return;
        }

        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached) await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        var previousGag = currentGagData.GetGagType(dto.Layer);
        var previousPadlock = currentGagData.GetGagPadlock(dto.Layer);

        switch (dto.Type)
        {
            case GagUpdateType.Applied:
                if (!pairPerms.ApplyGags || !currentGagData.CanApplyOrLockGag(dto.Layer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Apply Gags!").ConfigureAwait(false);
                    return;
                }
                currentGagData.NewGagType(dto.Layer, dto.Gag.GagName());
                break;

            case GagUpdateType.Locked:
                if (!pairPerms.LockGags || !currentGagData.CanApplyOrLockGag(dto.Layer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Lock Gags!").ConfigureAwait(false);
                    return;
                }

                // a little messy due to not wanting to parse timespan twice but can cleanup later when a better lock validation system is in place.
                // (so we dont need to do logic on the server)
                var lockCode = currentGagData.GetGagPadlock(dto.Layer) switch
                {
                    Padlocks.None => PadlockReturnCode.NoPadlockSelected,
                    Padlocks.MetalPadlock => PadlockReturnCode.Success,
                    Padlocks.FiveMinutesPadlock => PadlockReturnCode.Success,
                    Padlocks.CombinationPadlock => GsPadlockEx.ValidateCombinationPadlock(dto.Password, pairPerms.PermanentLocks),
                    Padlocks.PasswordPadlock => GsPadlockEx.ValidatePasswordPadlock(dto.Password, pairPerms.PermanentLocks),
                    Padlocks.TimerPadlock => GsPadlockEx.ValidateTimerPadlock(dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.TimerPasswordPadlock => GsPadlockEx.ValidatePasswordTimerPadlock(dto.Password, dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.OwnerPadlock => GsPadlockEx.ValidateOwnerPadlock(pairPerms.OwnerLocks, pairPerms.PermanentLocks),
                    Padlocks.OwnerTimerPadlock => GsPadlockEx.ValidateOwnerTimerPadlock(pairPerms.OwnerLocks, dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.DevotionalPadlock => GsPadlockEx.ValidateDevotionalPadlock(pairPerms.DevotionalLocks, pairPerms.PermanentLocks),
                    Padlocks.DevotionalTimerPadlock => GsPadlockEx.ValidateDevotionalTimerPadlock(pairPerms.DevotionalLocks, dto.Timer, pairPerms.MaxGagTime),
                    _ => PadlockReturnCode.NoPadlockSelected
                };
                if (lockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Lock:"+ lockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }

                // update the lock.
                currentGagData.NewPadlock(dto.Layer, dto.Padlock.ToName());
                currentGagData.NewPassword(dto.Layer, dto.Password);
                currentGagData.NewTimer(dto.Layer, dto.Timer);
                currentGagData.NewAssigner(dto.Layer, dto.Assigner);
                break;

            case GagUpdateType.Unlocked:
                if (!pairPerms.UnlockGags) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Unlock Gags!").ConfigureAwait(false);
                    return;
                }
                // validate if we can unlock the gag, if not, throw a warning.
                var unlockCode = GsPadlockEx.ValidateUnlock(currentGagData.ToGagSlot(dto.Layer), new(currentGagData.UserUID), dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (unlockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Unlock:"+ unlockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the unlock.
                currentGagData.NewPadlock(dto.Layer, Padlocks.None.ToName());
                currentGagData.NewPassword(dto.Layer, string.Empty);
                currentGagData.NewTimer(dto.Layer, DateTimeOffset.MinValue);
                currentGagData.NewAssigner(dto.Layer, string.Empty);
                break;

            case GagUpdateType.Removed:
                if (!pairPerms.RemoveGags || currentGagData.CanRemoveGag(dto.Layer)) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Not allowed access to Remove Gags!").ConfigureAwait(false);
                    return;
                }
                currentGagData.NewGagType(dto.Layer, GagType.None.GagName());
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Appearance Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Appearance Data: " + dto.Type);
                return;
        }

        // update the database with the new appearance data.
        DbContext.UserGagData.Update(currentGagData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newGagData = currentGagData.ToApiGagData();

        var recipientDto = new OnlineUserGagDataDto(new(dto.User.UID), new(UserUID), newGagData, dto.Type, UpdateDir.Other)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        // send back to recipient.
        await Clients.User(dto.User.UID).Client_UserReceiveDataAppearance(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        // send back to all recipients pairs.
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataAppearance(recipientDto with { Direction = UpdateDir.Other }).ConfigureAwait(false);
    }

    public async Task UserPushPairDataRestraint(PushPairRestraintDataUpdateDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Fetch affected pair's current activeState data from the DB
        var userActiveSet = await DbContext.UserActiveSetData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (userActiveSet == null) throw new Exception("User has no Active State Data!");

        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached) await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        var prevSetId = userActiveSet.ActiveSetId;
        var prevPadlock = userActiveSet.Padlock.ToPadlock();

        switch (dto.Type)
        {
            case WardrobeUpdateType.Applied:
                if (!pairPerms.ApplyRestraintSets || !userActiveSet.CanApplyRestraint()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Replace Currently Active Set because it is currently locked!").ConfigureAwait(false);
                    return;
                }
                userActiveSet.ActiveSetId = dto.ActiveSetId;
                userActiveSet.ActiveSetEnabler = dto.Enabler;
                break;

            case WardrobeUpdateType.Locked:
                if (!pairPerms.LockRestraintSets || !userActiveSet.CanLockRestraint()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Lock Set!").ConfigureAwait(false);
                    return;
                }
                // a little messy due to not wanting to parse timespan twice but can cleanup later when a better lock validation system is in place.
                // (so we dont need to do logic on the server)
                var lockCode = userActiveSet.Padlock.ToPadlock() switch
                {
                    Padlocks.None => PadlockReturnCode.NoPadlockSelected,
                    Padlocks.MetalPadlock => PadlockReturnCode.Success,
                    Padlocks.FiveMinutesPadlock => PadlockReturnCode.Success,
                    Padlocks.CombinationPadlock => GsPadlockEx.ValidateCombinationPadlock(dto.Password, pairPerms.PermanentLocks),
                    Padlocks.PasswordPadlock => GsPadlockEx.ValidatePasswordPadlock(dto.Password, pairPerms.PermanentLocks),
                    Padlocks.TimerPadlock => GsPadlockEx.ValidateTimerPadlock(dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.TimerPasswordPadlock => GsPadlockEx.ValidatePasswordTimerPadlock(dto.Password, dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.OwnerPadlock => GsPadlockEx.ValidateOwnerPadlock(pairPerms.OwnerLocks, pairPerms.PermanentLocks),
                    Padlocks.OwnerTimerPadlock => GsPadlockEx.ValidateOwnerTimerPadlock(pairPerms.OwnerLocks, dto.Timer, pairPerms.MaxGagTime),
                    Padlocks.DevotionalPadlock => GsPadlockEx.ValidateDevotionalPadlock(pairPerms.DevotionalLocks, pairPerms.PermanentLocks),
                    Padlocks.DevotionalTimerPadlock => GsPadlockEx.ValidateDevotionalTimerPadlock(pairPerms.DevotionalLocks, dto.Timer, pairPerms.MaxGagTime),
                    _ => PadlockReturnCode.NoPadlockSelected
                };
                if (lockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Lock:" + lockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the stuff.
                userActiveSet.Padlock = dto.Padlock.ToName();
                userActiveSet.Password = dto.Password;
                userActiveSet.Timer = dto.Timer;
                userActiveSet.Assigner = dto.Assigner;
                break;

            case WardrobeUpdateType.Unlocked:
                if(!pairPerms.UnlockRestraintSets || !userActiveSet.CanUnlockRestraint()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Unlock Set!").ConfigureAwait(false);
                    return;
                }

                // see if we are able to unlock the restraint, and if we are, do so, otherwise, return the error code.
                var unlockCode = GsPadlockEx.ValidateUnlock(userActiveSet, new(dto.Recipient.UID), dto.Password, UserUID, pairPerms.OwnerLocks, pairPerms.DevotionalLocks);
                if (unlockCode != PadlockReturnCode.Success) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Error Validating Unlock: " + unlockCode.ToFlagString()).ConfigureAwait(false);
                    return;
                }
                // update the stuff.
                userActiveSet.Padlock = Padlocks.None.ToName();
                userActiveSet.Password = string.Empty;
                userActiveSet.Timer = DateTimeOffset.MinValue;
                userActiveSet.Assigner = string.Empty;
                break;

            case WardrobeUpdateType.Disabled:
                if (!pairPerms.RemoveRestraintSets || !userActiveSet.CanRemoveRestraint()) {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Remove Set!").ConfigureAwait(false);
                    return;
                }
                // update the stuff.
                userActiveSet.ActiveSetId = Guid.Empty;
                userActiveSet.ActiveSetEnabler = string.Empty;
                break;

            default:
                throw new Exception("Invalid UpdateKind for Wardrobe Data!");
        }

        // update the changes to the database.
        DbContext.UserActiveSetData.Update(userActiveSet);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var updatedWardrobeData = userActiveSet.ToApiActiveSetData();

        // send the update to the recipient.
        var recipientDto = new OnlineUserRestraintDataDto(dto.Recipient, new(UserUID), updatedWardrobeData, dto.Type, UpdateDir.Other)
        {
            RestraintModified = prevSetId,
            PreviousPadlock = prevPadlock
        };

        await Clients.User(dto.User.UID).Client_UserReceiveDataWardrobe(recipientDto with { Direction = UpdateDir.Own }).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataWardrobe(recipientDto).ConfigureAwait(false);
    }

    public async Task UserPushPairDataAliasStorage(PushPairAliasDataUpdateDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // verify that a pair between the two clients is made.
        var pairPerms = await DbContext.ClientPairs.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // ensure that the update kind of to change the registered names. If it is not, throw exception.
        if (dto.Type is not PuppeteerUpdateType.PlayerNameRegistered) throw new Exception("Invalid UpdateKind for Pair Alias Data!");

        // in our dto, we have the PAIR WE ARE PROVIDING OUR NAME TO as the user-data, with our name info inside.
        // so when we construct the message to update the client's OWN data, we need to place the client callers name info inside.
        await Clients.User(dto.User.UID).Client_UserReceiveDataAlias(new(new(dto.User.UID), new(UserUID), dto.LastAliasData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Caller.Client_UserReceiveDataAlias(new(new(dto.User.UID), new(UserUID), dto.LastAliasData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
    }

    public async Task UserPushPairDataToybox(PushPairToyboxDataUpdateDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPerms = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

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

        // we need to update our lists based on the interacted item, and store what item was interacted with.
        switch (dto.Type)
        {
            case ToyboxUpdateType.PatternExecuted:
                dto.LastToyboxData.ActivePattern = dto.AffectedIdentifier;
                break;

            case ToyboxUpdateType.PatternStopped:
                dto.LastToyboxData.ActivePattern = Guid.Empty;
                break;

            case ToyboxUpdateType.AlarmToggled:
                if (dto.LastToyboxData.ActiveAlarms.Contains(dto.AffectedIdentifier))
                    dto.LastToyboxData.ActiveAlarms.Remove(dto.AffectedIdentifier);
                else
                    dto.LastToyboxData.ActiveAlarms.Add(dto.AffectedIdentifier);
                break;

            case ToyboxUpdateType.TriggerToggled:
                if (dto.LastToyboxData.ActiveTriggers.Contains(dto.AffectedIdentifier))
                    dto.LastToyboxData.ActiveTriggers.Remove(dto.AffectedIdentifier);
                else
                    dto.LastToyboxData.ActiveTriggers.Add(dto.AffectedIdentifier);
                break;

            default:
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Invalid UpdateKind for Toybox Data: " + dto.Type).ConfigureAwait(false);
                _logger.LogWarning("Invalid UpdateKind for Toybox Data: " + dto.Type);
                return;
        }

        await Clients.User(dto.User.UID).Client_UserReceiveDataToybox(new(dto.Recipient, new(UserUID), dto.LastToyboxData, dto.Type, UpdateDir.Own)).ConfigureAwait(false);
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveDataToybox(new(dto.Recipient, new(UserUID), dto.LastToyboxData, dto.Type, UpdateDir.Other)).ConfigureAwait(false);
    }
}
#pragma warning restore MA0051 // Method is too long
