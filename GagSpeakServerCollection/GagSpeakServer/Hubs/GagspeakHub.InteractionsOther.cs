using GagspeakAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveGag(PushKinksterActiveGagSlot dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // GagData must exist.
        if (await DbContext.UserGagData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false) is not { } currentGagData)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

        // Store previous values.
        GagType previousGag = currentGagData.Gag;
        Padlocks previousPadlock = currentGagData.Padlock;

        // Update based on DataUpdateType.
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

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Obtain the recipients to send the data to now that we know we can.
        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        // Construct return objects. 
        ActiveGagSlot newGagData = currentGagData.ToApiGagSlot();
        KinksterUpdateActiveGag recipientDto = new(new(dto.User.UID), new(UserUID), newGagData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        await Clients.Users([ ..onlinePairUids, dto.User.UID]).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferGags);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveRestriction(PushKinksterActiveRestriction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Must have a restriction data set.
        if (await DbContext.UserRestrictionData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID && u.Layer == dto.Layer).ConfigureAwait(false) is not { } curRestrictionData)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

        // Store previous values.
        Guid prevRestriction = curRestrictionData.Identifier;
        Padlocks prevPadlock = curRestrictionData.Padlock;

        // Update based on DataUpdateType.
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

        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        ActiveRestriction newRestrictionData = curRestrictionData.ToApiRestrictionSlot();
        KinksterUpdateActiveRestriction recipientDto = new(new(dto.User.UID), new(UserUID), newRestrictionData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevRestriction,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users([.. onlinePairUids, dto.User.UID]).Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestrictions);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveRestraint(PushKinksterActiveRestraint dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Must have a restraint data set.
        if (await DbContext.UserRestraintData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } curRestraintSetData)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Store previous values.
        var prevRestraintSet = curRestraintSetData.Identifier;
        var prevLayers = curRestraintSetData.ActiveLayers;
        var prevPadlock = curRestraintSetData.Padlock;

        // Update based on DataUpdateType.
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

        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        CharaActiveRestraint updatedWardrobeData = curRestraintSetData.ToApiRestraintData();
        KinksterUpdateActiveRestraint recipientDto = new(dto.User, new(UserUID), updatedWardrobeData, dto.Type)
        {
            PreviousRestraint = prevRestraintSet,
            PrevLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users([.. onlinePairUids, dto.Target.UID]).Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestraint);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveCollar(PushKinksterActiveCollar dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (!await DbContext.ClientPairs.AnyAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Target must be collared to do this.
        if (await DbContext.UserCollarData.Include(c => c.Owners).FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } collar)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Caller must be one of the Collar Owners.
        if (!collar.Owners.Any(o => o.OwnerUID.Equals(UserUID, StringComparison.Ordinal)))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotCollarOwner);

        // Mark prev collar and perform changes.
        var prevCollar = collar.Identifier;

        // Must reject if a change is attempted that Collared Kinkster does not have access to.
        switch (dto.Type)
        {
            case DataUpdateType.VisibilityChange:
                if (!collar.OwnerEditAccess.HasAny(CollarAccess.Visuals))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.Visuals = !collar.Visuals;
                break;

            case DataUpdateType.DyesChange:
                if (!collar.OwnerEditAccess.HasAny(CollarAccess.Dyes))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.Dye1 = dto.Dye1;
                collar.Dye2 = dto.Dye2;
                break;

            case DataUpdateType.CollarMoodleChange:
                if (!collar.OwnerEditAccess.HasAny(CollarAccess.Moodle))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.MoodleId = dto.Moodle.GUID;
                collar.MoodleIconId = dto.Moodle.IconID;
                collar.MoodleTitle = dto.Moodle.Title;
                collar.MoodleDescription = dto.Moodle.Description;
                collar.MoodleType = (byte)dto.Moodle.Type;
                collar.MoodleVFXPath = dto.Moodle.CustomVFXPath;
                break;

            case DataUpdateType.CollarWritingChange:
                if (!collar.OwnerEditAccess.HasAny(CollarAccess.Writing))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.Writing = dto.Writing;
                break;

            case DataUpdateType.CollarRemoved:
                // maybe add some safeguard down the line here, but
                // this should be easily triggerable with a safeword for obvious reasons.
                collar.Identifier = Guid.Empty;
                collar.Visuals = true; // reset visuals to true.
                collar.Dye1 = 0; // reset dye1 to default.
                collar.Dye2 = 0; // reset dye2 to default.
                collar.MoodleId = Guid.Empty; // reset moodle to default.
                collar.MoodleIconId = 0; // reset moodle icon to default.
                collar.MoodleTitle = string.Empty; // reset moodle title to default.
                collar.MoodleDescription = string.Empty; // reset moodle description to default.
                collar.MoodleType = 0; // reset moodle type to default.
                collar.MoodleVFXPath = string.Empty; // reset moodle vfx path to default.
                collar.Writing = string.Empty; // reset writing to default.
                collar.EditAccess = CollarAccess.None; // reset edit access to none.
                collar.OwnerEditAccess = CollarAccess.None; // reset owner edit access to none.
                // remove all owners.
                DbContext.CollarOwners.RemoveRange(collar.Owners);
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // Grabs all Pairs of the affected pair
        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        var newData = collar.ToApiCollarData();
        var callbackDto = new KinksterUpdateActiveCollar(dto.User, new(UserUID), newData, dto.Type)
        {
            PreviousCollar = prevCollar
        };

        await Clients.Users([..onlinePairUids, dto.Target.UID]).Callback_KinksterUpdateActiveCollar(callbackDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferCollar);
        return HubResponseBuilder.Yippee();
    }


    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActivePattern(PushKinksterActivePattern dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Validate change based on DataUpdateType.
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
        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, dto.Target.UID]).Callback_KinksterUpdateActivePattern(new(dto.Target, new(UserUID), dto.ActivePattern, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferPattern);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveAlarms(PushKinksterActiveAlarms dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be correct update type.
        if (dto.Type is not DataUpdateType.AlarmToggled)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Permission must be granted.
        if (!pairPerms.ToggleAlarms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, dto.Target.UID ]).Callback_KinksterUpdateActiveAlarms(new(dto.Target, new(UserUID), dto.ActiveAlarms, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferAlarms);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeKinksterActiveTriggers(PushKinksterActiveTriggers dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.Target.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be correct update type.
        if (dto.Type is not DataUpdateType.TriggerToggled)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.Target.UID && string.Equals(p.OtherUserUID, UserUID)).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Permission must be granted.
        if (!pairPerms.ToggleTriggers)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.Target.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([..onlinePairUids, dto.Target.UID]).Callback_KinksterUpdateActiveTriggers(new(dto.Target, new(UserUID), dto.ActiveTriggers, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferTriggers);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserSendNameToKinkster(KinksterBase dto, string listenerName)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (!await DbContext.ClientPairs.AnyAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Give target name.
        await Clients.User(dto.User.UID).Callback_ListenerName(new(UserUID), listenerName).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterNamesSent);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOtherGlobalPerm(SingleChangeGlobal dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Perms must exist.
        if (await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Change must be valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs that the paired user we are updating has.
        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, dto.User.UID]).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeGlobal);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOtherPairPerm(SingleChangeUnique dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Must be paired.
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Change must be valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.User(dto.User.UID).Callback_SingleChangeUnique(new(new(UserUID), dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        await Clients.Caller.Callback_SingleChangeUnique(new(dto.User, dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeUnique);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOtherHardcoreState(HardcoreStateChange dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Reject hypnosis.
        if (dto.Changed is HcAttribute.HypnoticEffect)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        // Cannot update self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Pair Permissions must exist.
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Hardcore State must exist.
        if (await DbContext.UserHardcoreState.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } hcState)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Make changes based on the attribute type, if allowed.
        switch (dto.Changed)
        {
            case HcAttribute.Follow:
                if (!pairPerms.AllowLockedFollowing)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!hcState.CanChange(HcAttribute.Follow, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // State changes must be toggled
                var followActive = !string.IsNullOrEmpty(hcState.LockedFollowing);
                if (followActive && dto.NewData.LockedFollowing.Length > 0 || !followActive && dto.NewData.LockedFollowing == string.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);

                // perform the updates on the values.
                hcState.LockedFollowing = dto.NewData.LockedFollowing;
                break;

            case HcAttribute.EmoteState:
                var sitEmoteAllowed = pairPerms.AllowLockedSitting;
                var anyEmoteAllowed = pairPerms.AllowLockedEmoting;
                if (!sitEmoteAllowed && !anyEmoteAllowed)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                // if emoteID is not a sit emote and we dont have locked emoting, reject.
                if (!dto.NewData.InAnySitEmote() && !anyEmoteAllowed)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                // we are able to swap between emotes if allowed, but validate with devotional lock first.
                if (!hcState.CanChange(HcAttribute.EmoteState, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);
                // Update the state.
                hcState.LockedEmoteState = dto.NewData.LockedEmoteState;
                hcState.EmoteExpireTime = dto.NewData.EmoteExpireTime;
                hcState.EmoteId = dto.NewData.EmoteId;
                hcState.EmoteCyclePose = dto.NewData.EmoteCyclePose;
                break;

            case HcAttribute.Confinement:
                if (!pairPerms.AllowIndoorConfinement)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!hcState.CanChange(HcAttribute.Confinement, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // State changes must be toggled between on and off, not swapped.
                var isActive = !string.IsNullOrEmpty(hcState.IndoorConfinement);
                if (isActive && dto.NewData.LockedFollowing.Length > 0 || !isActive && dto.NewData.LockedFollowing == string.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);

                // perform the updates on the values.
                hcState.IndoorConfinement = dto.NewData.IndoorConfinement;
                hcState.ConfinementTimer = dto.NewData.ConfinementTimer;
                hcState.ConfinedWorld = dto.NewData.ConfinedWorld;
                hcState.ConfinedCity = dto.NewData.ConfinedCity;
                hcState.ConfinedWard = dto.NewData.ConfinedWard;
                hcState.ConfinedPlaceId = dto.NewData.ConfinedPlaceId;
                hcState.ConfinedInApartment = dto.NewData.ConfinedInApartment;
                hcState.ConfinedInSubdivision = dto.NewData.ConfinedInSubdivision;
                break;

            case HcAttribute.Imprisonment:
                if (!pairPerms.AllowImprisonment)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!hcState.CanChange(HcAttribute.Imprisonment, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                var currentlyImprisoned = hcState.Imprisonment.Length > 0;
                var willBeImprisoned = dto.NewData.Imprisonment.Length > 0;
                // if both of the above are true, then the territories must match, and cannot be > 30y apart.
                if (currentlyImprisoned && willBeImprisoned)
                {
                    // Must be in the same territory.
                    if (hcState.ImprisonedTerritory != dto.NewData.ImprisonedTerritory)
                        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);
                    // Must not be over 30y apart.
                    var prevPos = new Vector3(hcState.ImprisonedPosX, hcState.ImprisonedPosY, hcState.ImprisonedPosZ);
                    var newPos = new Vector3(dto.NewData.ImprisonedPos.X, dto.NewData.ImprisonedPos.Y, dto.NewData.ImprisonedPos.Z);
                    if (Vector3.Distance(prevPos, newPos) > 30f)
                        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);
                }

                // perform the updates on the values.
                hcState.Imprisonment = dto.NewData.Imprisonment;
                hcState.ImprisonmentTimer = dto.NewData.ImprisonmentTimer;
                hcState.ImprisonedTerritory = dto.NewData.ImprisonedTerritory;
                hcState.ImprisonedPosX = dto.NewData.ImprisonedPos.X;
                hcState.ImprisonedPosY = dto.NewData.ImprisonedPos.Y;
                hcState.ImprisonedPosZ = dto.NewData.ImprisonedPos.Z;
                hcState.ImprisonedRadius = dto.NewData.ImprisonedRadius;
                break;

            case HcAttribute.HiddenChatBox:
                if (!pairPerms.AllowHidingChatBoxes)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!hcState.CanChange(HcAttribute.HiddenChatBox, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // State changes must be toggled
                var isChatHidden = !string.IsNullOrEmpty(hcState.ChatBoxesHidden);
                if (isChatHidden && dto.NewData.ChatBoxesHidden.Length > 0 || !isChatHidden && dto.NewData.LockedFollowing == string.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);
                // perform the updates on the values.
                hcState.ChatBoxesHidden = dto.NewData.ChatBoxesHidden;
                hcState.ChatBoxesHiddenTimer = dto.NewData.ChatBoxesHiddenTimer;
                break;

            case HcAttribute.HiddenChatInput:
                if (!pairPerms.AllowHidingChatInput)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

                if (!hcState.CanChange(HcAttribute.HiddenChatInput, UserUID))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);

                // State changes must be toggled
                var isInputHidden = !string.IsNullOrEmpty(hcState.ChatInputHidden);
                if (isInputHidden && dto.NewData.ChatInputHidden.Length > 0 || !isInputHidden && dto.NewData.ChatInputHidden == string.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);

                // perform the updates on the values.
                hcState.ChatInputHidden = dto.NewData.ChatInputHidden;
                hcState.ChatInputHiddenTimer = dto.NewData.ChatInputHiddenTimer;
                break;

            case HcAttribute.BlockedChatInput:
                if (!pairPerms.AllowChatInputBlocking)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                if (!hcState.CanChange(HcAttribute.BlockedChatInput, UserUID))

                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotItemAssigner);
                // State changes must be toggled
                var isInputBlocked = !string.IsNullOrEmpty(hcState.ChatInputBlocked);
                if (isInputBlocked && dto.NewData.ChatInputBlocked.Length > 0 || !isInputBlocked && dto.NewData.ChatInputBlocked == string.Empty)
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);
                // perform the updates on the values.
                hcState.ChatInputBlocked = dto.NewData.ChatInputBlocked;
                hcState.ChatInputBlockedTimer = dto.NewData.ChatInputBlockedTimer;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);

        }

        DbContext.Update(hcState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newData = hcState.ToApiHardcoreState();

        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([..onlinePairUids, dto.User.UID]).Callback_StateChangeHardcore(new(dto.User, newData, dto.Changed, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeHardcore);
        return HubResponseBuilder.Yippee();
    }

    /// <summary>
    ///     Sends a custom Hypnosis effect to another kinkster, which they will execute. <para />
    ///     If the image string is not null, ensure they have permission rights to do so. <para />
    ///     When received by the target, they should update their HardcoreState to reflect these changes.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserHypnotizeKinkster(HypnoticAction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Cannot be self-targeted.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // HcState must exist.
        if (await DbContext.UserHardcoreState.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } hcState)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Current Hypno Effect must be empty (maybe make changeable later)
        if (hcState.HypnoticEffect.Length > 0)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState);

        // Must be paired. (Obtain targets perms for us)
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // If sending custom image, must have permission.
        if (!string.IsNullOrEmpty(dto.base64Image) && !pairPerms.AllowHypnoImageSending)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        // Must have permission to send any effect.
        if (!pairPerms.HypnoEffectSending)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        hcState.HypnoticEffect = pairPerms.DevotionalLocks ? $"{UserUID}{Constants.DevotedString}" : UserUID;
        hcState.HypnoticEffectTimer = dto.ExpireTime;

        DbContext.Update(hcState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newHcState = hcState.ToApiHardcoreState();

        // Otherwise we can set it (assuming the permissions are valid) and should also inform all of this pair's pairs.
        var pairsOfTarget = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        // Inform all pairs of target to simply update the hardcore state.
        await Clients.Users(onlinePairUids).Callback_StateChangeHardcore(new(dto.User, newHcState, HcAttribute.HypnoticEffect, new(UserUID))).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeHardcore);

        // For the target user, hypnotize them with the full effect.
        await Clients.User(dto.User.UID).Callback_HypnoticEffect(new(new(UserUID), dto.ExpireTime, dto.Effect, dto.base64Image)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterHypnoticEffectsSent);
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
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

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

        // if they are, we should ask them to remove the client pair from their listing as well.
        await Clients.User(dto.User.UID).Callback_RemoveClientPair(new(new(UserUID))).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

}