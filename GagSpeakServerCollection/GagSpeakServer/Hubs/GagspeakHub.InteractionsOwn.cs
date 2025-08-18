using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
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
    public async Task<HubResponse> UserPushActiveData(PushClientCompositeUpdate dto)
    {
        _logger.LogCallInfo();
        var recipientUids = dto.Recipients.Select(r => r.UID);
        
        // if a safeword, we need to clear all the data for the appearance and activeSetData.
        if (dto.WasSafeword)
        {
            _logger.LogWarning($"FOR SOME REASON, {UserUID} SAFEWORDED! Clearing all gag data, restriction data, and restraint data for them.");
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
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count()));
            await Clients.Users(recipientUids).Callback_KinksterUpdateComposite(new(new(UserUID), dto.NewData, dto.WasSafeword)).ConfigureAwait(false);
            _metrics.IncCounter(MetricsAPI.CounterSafewordUsed);
        }
        else
        {
            // Push the composite data off to the other pairs.
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count()));
            await Clients.Users(recipientUids).Callback_KinksterUpdateComposite(new(new(UserUID), dto.NewData, dto.WasSafeword)).ConfigureAwait(false);
        }

        _metrics.IncCounter(MetricsAPI.CounterStateTransferFull);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveGags(PushClientActiveGagSlot dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

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
        KinksterUpdateActiveGag recipientDto = new(new(UserUID), new(UserUID), newAppearance, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousGag = previousGag,
            PreviousPadlock = previousPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferGags);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveRestrictions(PushClientActiveRestriction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

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
        KinksterUpdateActiveRestriction recipientDto = new(new(UserUID), new(UserUID), newRestrictionData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevId,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestrictions);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveRestraint(PushClientActiveRestraint dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

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
                curRestraintData.ActiveLayers = RestraintLayer.None;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind);
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restraintData.
        CharaActiveRestraint newRestraintData = curRestraintData.ToApiRestraintData();
        KinksterUpdateActiveRestraint recipientDto = new(new(UserUID), new(UserUID), newRestraintData, dto.Type)
        {
            PreviousRestraint = prevSetId,
            PrevLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestraint);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveCollar(PushClientActiveCollar dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

        // Client must be collared to do this.
        UserCollarData? collar = dto.Type is DataUpdateType.CollarRemoved
            ? await DbContext.UserCollarData.Include(c => c.Owners).FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false)
            : await DbContext.UserCollarData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (collar is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotCollared);

        var prevCollar = collar.Identifier;

        // Must reject if a change is attempted that Collared Kinkster does not have access to.
        switch (dto.Type)
        {
            case DataUpdateType.VisibilityChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Visuals))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.Visuals = !collar.Visuals;
                break;

            case DataUpdateType.DyesChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Dyes))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.Dye1 = dto.Dye1;
                collar.Dye2 = dto.Dye2;
                break;

            case DataUpdateType.CollarMoodleChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Moodle))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
                collar.MoodleId = dto.Moodle.GUID;
                collar.MoodleIconId = dto.Moodle.IconID;
                collar.MoodleTitle = dto.Moodle.Title;
                collar.MoodleDescription = dto.Moodle.Description;
                collar.MoodleType = (byte)dto.Moodle.Type;
                collar.MoodleVFXPath = dto.Moodle.CustomVFXPath;
                break;

            case DataUpdateType.CollarWritingChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Writing))
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

        // update and save collar data.
        DbContext.Update(collar);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Package to API and run callback.
        var newCollarData = collar.ToApiCollarData();
        var callbackDto = new KinksterUpdateActiveCollar(new(UserUID), new(UserUID), newCollarData, dto.Type)
        {
            PreviousCollar = dto.Type is DataUpdateType.CollarRemoved ? prevCollar : Guid.Empty
        };
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveCollar(callbackDto).ConfigureAwait(false);
        await Clients.Caller.Callback_KinksterUpdateActiveCollar(callbackDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferCollar);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveLoot(PushClientActiveLoot dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        // if the loot is not null, we assume application, so try and apply it if a gag item.
        if (dto.LootItem is { } item && item.Type is CursedLootType.Gag && item.Gag.HasValue)
        {
            // grab the usergagdata for the first open slot with no gag item applied, if no layers are available, return invalid layer.
            UserGagData? curGagData = await DbContext.UserGagData.FirstOrDefaultAsync(data => data.UserUID == UserUID && data.Gag == GagType.None).ConfigureAwait(false);
            if (curGagData is null)
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer);

            // update the data.
            curGagData.Gag = item.Gag.Value;
            curGagData.Enabler = "Mimic";
            curGagData.Padlock = Padlocks.Mimic;
            curGagData.Password = string.Empty;
            curGagData.Timer = dto.LootItem.ReleaseTimeUTC;
            curGagData.PadlockAssigner = "Mimic";

            // save changes to our tracked item.
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            ActiveGagSlot newAppearance = curGagData.ToApiGagSlot();
            KinksterUpdateActiveGag recipientDto = new(new(UserUID), new(UserUID), newAppearance, DataUpdateType.AppliedCursed)
            {
                AffectedLayer = curGagData.Layer,
                PreviousGag = GagType.None,
                PreviousPadlock = Padlocks.None
            };
            // Return Gag Update.
            await Clients.Users(recipientUids).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
            await Clients.Caller.Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        }

        // Push CursedLoot update to all recipients.
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveCursedLoot(new(new(UserUID), dto.ActiveItems, dto.ChangeItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferLoot);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushAliasGlobalUpdate(PushClientAliasGlobalUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateAliasGlobal(new(new(UserUID), dto.AliasId, dto.NewData)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushAliasUniqueUpdate(PushClientAliasUniqueUpdate dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        await Clients.User(dto.Recipient.UID).Callback_KinksterUpdateAliasUnique(new(new(UserUID), dto.AliasId, dto.NewData)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushValidToys(PushClientValidToys dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateValidToys(new(new(UserUID), dto.ValidToys)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferToys);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActivePattern(PushClientActivePattern dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateActivePattern(new(new(UserUID), new(UserUID), dto.ActivePattern, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferPattern);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveAlarms(PushClientActiveAlarms dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // convert the recipient UID list from the recipient list of the dto
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveAlarms(new(new(UserUID), new(UserUID), dto.ActiveAlarms, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferAlarms);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveTriggers(PushClientActiveTriggers dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveTriggers(new(new(UserUID), new(UserUID), dto.ActiveTriggers, dto.ChangedItem, dto.Type)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferTriggers);
        return HubResponseBuilder.Yippee();
    }

    // Pushing updates to client data items.

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewGagData(PushClientDataChangeGag dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewGagData(new(new(UserUID), dto.GagType, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateGags);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewRestrictionData(PushClientDataChangeRestriction dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewRestrictionData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateRestrictions);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewRestraintData(PushClientDataChangeRestraint dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewRestraintData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateRestraint);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewCollarData(PushClientDataChangeCollar dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewCollarData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateCollar);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewLootData(PushClientDataChangeLoot dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewLootData(new(new(UserUID), dto.Id, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateLoot);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewPatternData(PushClientDataChangePattern dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewPatternData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdatePattern);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewAlarmData(PushClientDataChangeAlarm dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewAlarmData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateAlarms);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewTriggerData(PushClientDataChangeTrigger dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewTriggerData(new(new(UserUID), dto.ItemId, dto.LightItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateTriggers);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushNewAllowances(PushClientAllowances dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewAllowances(new(new(UserUID), dto.Module, dto.AllowedUids)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateAllowances);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserBulkChangeGlobal(BulkChangeGlobal dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Cannot update anyone but self.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Globals must exist
        if (await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } globals)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // HardcoreState must exist.
        if (await DbContext.UserHardcoreState.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } hardcoreState)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData); 

        // Update the permissions to the new values.
        UserGlobalPermissions newGlobals = dto.NewPerms.ToModelGlobalPerms(globals);
        UserHardcoreState newHardcore = dto.NewState.ToModelHardcoreState(hardcoreState);
        DbContext.Update(newGlobals);
        DbContext.Update(newHardcore);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        var pairsOfTarget = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_BulkChangeGlobal(new(new(UserUID), dto.NewPerms, dto.NewState)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeGlobal);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserBulkChangeUnique(BulkChangeUnique dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Target user cannot be self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // KinksterPair must exist.
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Access must exist.
        if (await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairAccess)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Set, Update, & Save.
        ClientPairPermissions newPairPerms = dto.NewPerms.ToModelKinksterPerms(pairPerms);
        ClientPairPermissionAccess newPairAccess = dto.NewAccess.ToModelKinksterEditAccess(pairAccess);
        DbContext.Update(newPairPerms);
        DbContext.Update(newPairAccess);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Callback to caller and pair of success. (In Future note that since we now do callback messages we can do this locally with no callback needed for instant change).
        await Clients.Caller.Callback_BulkChangeUnique(new(dto.User, dto.NewPerms, dto.NewAccess, dto.Direction, dto.Enactor)).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Callback_BulkChangeUnique(new(new(UserUID), dto.NewPerms, dto.NewAccess, dto.Direction, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeUnique);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnGlobalPerm(SingleChangeGlobal dto)
    {
        // _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Caller must be the same as the target.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Perms must exist.
        if (await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Change must be valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        // Otherwise, we correctly set the property and updated things.
        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        var pairsOfTarget = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeGlobal);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnPairPerm(SingleChangeUnique dto)
    {
        // _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Permissions must exist.
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // store to see if change is a pause change
        bool prevPauseState = perms.IsPaused;

        // Ensure change is valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.User(UserUID).Callback_SingleChangeUnique(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Callback_SingleChangeUnique(new(new(UserUID), dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeUnique);

        // Handle sending offline/online if a pause was toggled.
        if (!(perms.IsPaused != prevPauseState))
            return HubResponseBuilder.Yippee();

        // Only perform if other side of pairPerms is valid.
        if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is { } otherPairData && !otherPairData.IsPaused)
        {
            if (await GetUserIdent(dto.User.UID).ConfigureAwait(false) is { } otherIdent && UserCharaIdent is not null)
            {
                if ((bool)dto.NewPerm.Value)
                {
                    await Clients.User(UserUID).Callback_KinksterOffline(new(new(dto.User.UID))).ConfigureAwait(false);
                    await Clients.User(dto.User.UID).Callback_KinksterOffline(new(new(UserUID))).ConfigureAwait(false);
                }
                else
                {
                    await Clients.User(UserUID).Callback_KinksterOnline(new(new(dto.User.UID), otherIdent)).ConfigureAwait(false);
                    await Clients.User(dto.User.UID).Callback_KinksterOnline(new(new(UserUID), UserCharaIdent)).ConfigureAwait(false);
                }
            }
        }

        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnPairPermAccess(SingleChangeAccess dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Permissions must exist.
        if (await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Property must be valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.User(dto.User.UID).Callback_SingleChangeAccess(new(new(UserUID), dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        await Clients.Caller.Callback_SingleChangeAccess(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeAccess);
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

