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
        var recipientUids = dto.Recipients.Select(r => r.UID);
        // if a safeword, we need to clear all the data for the appearance and activeSetData.
        if (dto.WasSafeword)
        {
            _logger.LogWarning($"FOR SOME REASON, {UserUID} SAFEWORDED! Clearing all gag data, restriction data, and restraint data for them.");
            // Grab the gag data ordered by layer.
            List<UserGagData> curGagData = await DbContext.ActiveGagData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            if (curGagData.Any(g => g is null))
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot clear Gag Data, it does not exist!").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
            }

            // Grab the restriction data ordered by layer.
            List<UserRestrictionData> curRestrictions = await DbContext.ActiveRestrictionData.Where(u => u.UserUID == UserUID).OrderBy(u => u.Layer).ToListAsync().ConfigureAwait(false);
            if (curRestrictions.Any(r => r is null))
            {
                await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot clear Active Restriction Data, it does not exist!").ConfigureAwait(false);
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
            }

            // Grab the restraint set data.
            UserRestraintData? curRestraint = await DbContext.ActiveRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (curRestraint is null)
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
            foreach (UserRestrictionData activeSetData in curRestrictions)
            {
                activeSetData.Identifier = Guid.Empty;
                activeSetData.Enabler = string.Empty;
                activeSetData.Padlock = Padlocks.None;
                activeSetData.Password = string.Empty;
                activeSetData.Timer = DateTimeOffset.MinValue;
                activeSetData.PadlockAssigner = string.Empty;
            }

            // Clear the restraintData.
            curRestraint.Identifier = Guid.Empty;
            curRestraint.Enabler = string.Empty;
            curRestraint.Padlock = Padlocks.None;
            curRestraint.Password = string.Empty;
            curRestraint.Timer = DateTimeOffset.MinValue;
            curRestraint.PadlockAssigner = string.Empty;

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
    public async Task<HubResponse<ActiveGagSlot>> UserPushActiveGags(PushClientActiveGagSlot dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

        // Grab the appearance data from the database at the layer we want to interact with.
        UserGagData? curGagData = await DbContext.ActiveGagData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false);
        if (curGagData is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new ActiveGagSlot());

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
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind, new ActiveGagSlot());
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
        _metrics.IncCounter(MetricsAPI.CounterStateTransferGags);
        return HubResponseBuilder.Yippee(newAppearance);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<ActiveRestriction>> UserPushActiveRestrictions(PushClientActiveRestriction dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

        if (await DbContext.ActiveRestrictionData.FirstOrDefaultAsync(u => u.UserUID == UserUID && u.Layer == dto.Layer).ConfigureAwait(false) is not { } curData)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new ActiveRestriction());

        // get the previous gag type and padlock from the current data.
        Guid prevId = curData.Identifier;
        Padlocks prevPadlock = curData.Padlock;

        // we can always assume that this is correct when applied by self.
        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curData.Identifier = dto.Identifier;
                curData.Enabler = dto.Enabler;
                break;

            case DataUpdateType.Locked:
                curData.Padlock = dto.Padlock;
                curData.Password = dto.Password;
                curData.Timer = dto.Timer;
                curData.PadlockAssigner = dto.Assigner;
                break;

            case DataUpdateType.Unlocked:
                curData.Padlock = Padlocks.None;
                curData.Password = string.Empty;
                curData.Timer = DateTimeOffset.UtcNow;
                curData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                curData.Identifier = Guid.Empty;
                curData.Enabler = string.Empty;
                break;


            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind, new ActiveRestriction());
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restrictionData.
        ActiveRestriction newRestrictionData = curData.ToApiRestrictionSlot();
        KinksterUpdateActiveRestriction recipientDto = new(new(UserUID), new(UserUID), newRestrictionData, dto.Type)
        {
            AffectedLayer = dto.Layer,
            PreviousRestriction = prevId,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveRestriction(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestrictions);
        return HubResponseBuilder.Yippee(newRestrictionData);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<CharaActiveRestraint>> UserPushActiveRestraint(PushClientActiveRestraint dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

        // grab the restraintSetData from the database.
        if (await DbContext.ActiveRestraintData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } curData)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new CharaActiveRestraint());

        Guid prevSetId = curData.Identifier;
        RestraintLayer prevLayers = curData.ActiveLayers;
        Padlocks prevPadlock = curData.Padlock;

        switch (dto.Type)
        {
            case DataUpdateType.Swapped:
            case DataUpdateType.Applied:
                curData.Identifier = dto.ActiveSetId;
                curData.Enabler = dto.Enabler;
                break;

            // No bitwise operations for right now, just raw updates.
            case DataUpdateType.LayersChanged:
            case DataUpdateType.LayersApplied:
            case DataUpdateType.LayersRemoved:
                curData.ActiveLayers = dto.ActiveLayers;
                break;

            case DataUpdateType.Locked:
                curData.Padlock = dto.Padlock;
                curData.Password = dto.Password;
                curData.Timer = dto.Timer;
                curData.PadlockAssigner = dto.Assigner;
                break;

            case DataUpdateType.Unlocked:
                curData.Padlock = Padlocks.None;
                curData.Password = string.Empty;
                curData.Timer = DateTimeOffset.UtcNow;
                curData.PadlockAssigner = string.Empty;
                break;

            case DataUpdateType.Removed:
                curData.Identifier = Guid.Empty;
                curData.Enabler = string.Empty;
                curData.ActiveLayers = RestraintLayer.None;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind, new CharaActiveRestraint());
        }

        // save changes to our tracked item.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // get the updated restraintData.
        CharaActiveRestraint newRestraintData = curData.ToApiRestraintData();
        KinksterUpdateActiveRestraint recipientDto = new(new(UserUID), new(UserUID), newRestraintData, dto.Type)
        {
            PreviousRestraint = prevSetId,
            PrevLayers = prevLayers,
            PreviousPadlock = prevPadlock
        };

        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveRestraint(recipientDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferRestraint);
        return HubResponseBuilder.Yippee(newRestraintData);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<CharaActiveCollar>> UserPushActiveCollar(PushClientActiveCollar dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);

        // Client must be collared to do this.
        UserCollarData? collar = dto.Type is DataUpdateType.CollarRemoved
            ? await DbContext.ActiveCollarData.Include(c => c.Owners).FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false)
            : await DbContext.ActiveCollarData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (collar is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotCollared, new CharaActiveCollar());

        // Must reject if a change is attempted that Collared Kinkster does not have access to.
        switch (dto.Type)
        {
            case DataUpdateType.VisibilityChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Visuals))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions, new CharaActiveCollar());
                collar.Visuals = !collar.Visuals;
                break;

            case DataUpdateType.DyesChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Dyes))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions, new CharaActiveCollar());
                collar.Dye1 = dto.Dye1;
                collar.Dye2 = dto.Dye2;
                break;

            case DataUpdateType.CollarMoodleChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Moodle))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions, new CharaActiveCollar());
                collar.MoodleId = dto.Moodle.GUID;
                collar.MoodleIconId = dto.Moodle.IconID;
                collar.MoodleTitle = dto.Moodle.Title;
                collar.MoodleDescription = dto.Moodle.Description;
                collar.MoodleType = dto.Moodle.Type;
                collar.MoodleVFXPath = dto.Moodle.CustomVFXPath;
                break;

            case DataUpdateType.CollarWritingChange:
                if (!collar.EditAccess.HasAny(CollarAccess.Writing))
                    return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions, new CharaActiveCollar());
                collar.Writing = dto.Writing;
                break;

            case DataUpdateType.CollarRemoved:
                // maybe add some safeguard down the line here, but
                // this should be easily triggerable with a safeword for obvious reasons.
                collar.Visuals = true; // reset visuals to true.
                collar.Dye1 = 0; // reset dye1 to default.
                collar.Dye2 = 0; // reset dye2 to default.
                collar.MoodleId = Guid.Empty; // reset moodle to default.
                collar.MoodleIconId = 0; // reset moodle icon to default.
                collar.MoodleTitle = string.Empty; // reset moodle title to default.
                collar.MoodleDescription = string.Empty; // reset moodle description to default.
                collar.MoodleType = StatusType.Positive; // reset moodle type to default.
                collar.MoodleVFXPath = string.Empty; // reset moodle vfx path to default.
                collar.Writing = string.Empty; // reset writing to default.
                collar.EditAccess = CollarAccess.None; // reset edit access to none.
                collar.OwnerEditAccess = CollarAccess.None; // reset owner edit access to none.
                // remove all owners.
                DbContext.CollarOwners.RemoveRange(collar.Owners);
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind, new CharaActiveCollar());
        }

        // update and save collar data.
        DbContext.Update(collar);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Package to API and run callback.
        var newCollarData = collar.ToApiCollarData();
        var callbackDto = new KinksterUpdateActiveCollar(new(UserUID), new(UserUID), newCollarData, dto.Type);
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveCollar(callbackDto).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferCollar);
        return HubResponseBuilder.Yippee(newCollarData);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<AppliedCursedItem>> UserPushActiveLoot(PushClientActiveLoot dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        var returnItem = new AppliedCursedItem(dto.ChangeItem);
        // if the loot is not null, we assume application, so try and apply it if a gag item.
        if (dto.LootItem is { } item && item.Type is CursedLootType.Gag && item.Gag.HasValue)
        {
            // grab the UserGagData for the first open slot with no gag item applied, if no layers are available, return invalid layer.
            if (await DbContext.ActiveGagData.FirstOrDefaultAsync(data => data.UserUID == UserUID && data.Gag == GagType.None).ConfigureAwait(false) is not { } curGagData)
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidLayer, new AppliedCursedItem(Guid.Empty));

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
            returnItem = returnItem with { GagLayer = curGagData.Layer };
            // Return Gag Update.
            await Clients.Users(recipientUids).Callback_KinksterUpdateActiveGag(recipientDto).ConfigureAwait(false);
        }

        // Push CursedLoot update to all recipients.
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveCursedLoot(new(new(UserUID), dto.ActiveItems, dto.ChangeItem)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterStateTransferLoot);
        return HubResponseBuilder.Yippee(returnItem);
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushAliasState(PushClientAliasState dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterUpdateAliasState(new(new(UserUID), dto.AliasId, dto.NewState)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushActiveAliases(PushClientActiveAliases dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID); 
        await Clients.Users(recipientUids).Callback_KinksterUpdateActiveAliases(new(new(UserUID), dto.ActiveItems)).ConfigureAwait(false);
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
        await Clients.Users(recipientUids).Callback_KinksterNewCollarData(new(new(UserUID), dto.LightItem)).ConfigureAwait(false);
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
    public async Task<HubResponse> UserPushNewAliasData(PushClientDataChangeAlias dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_KinksterNewAliasData(new(new(UserUID), dto.AliasId, dto.NewData)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterDataUpdateAlarms);
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
    public async Task<HubResponse<ClientGlobals>> UserBulkChangeGlobal(BulkChangeGlobal dto)
    {
        // Cannot update anyone but self.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient, new ClientGlobals(new GlobalPerms(), new GagspeakAPI.Data.Permissions.HardcoreStatus()));

        // Globals must exist
        if (await DbContext.GlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } globals)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new ClientGlobals(new GlobalPerms(), new GagspeakAPI.Data.Permissions.HardcoreStatus()));

        // HardcoreState must exist.
        if (await DbContext.HardcoreState.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } hardcoreState)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new ClientGlobals(new GlobalPerms(), new GagspeakAPI.Data.Permissions.HardcoreStatus()));

        // Update the permissions to the new values.
        GlobalPermissions newGlobals = dto.NewPerms.ToModel(globals);
        HardcoreState newHardcore = dto.NewState.ToModel(hardcoreState);
        DbContext.Update(newGlobals);
        DbContext.Update(newHardcore);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        var pairsOfTarget = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var onlinePairsOfTarget = await GetOnlineUsers(pairsOfTarget).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfTarget.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_BulkChangeGlobal(new(new(UserUID), dto.NewPerms, dto.NewState)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeGlobal);
        return HubResponseBuilder.Yippee(new ClientGlobals(dto.NewPerms, dto.NewState));
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserBulkChangeUnique(BulkChangeUnique dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Target user cannot be self.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // KinksterPair must exist.
        if (await DbContext.PairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Access must exist.
        if (await DbContext.PairAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } pairAccess)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Set, Update, & Save.
        PairPermissions newPairPerms = dto.NewPerms.ToModel(pairPerms);
        PairPermissionAccess newPairAccess = dto.NewAccess.ToModel(pairAccess);
        DbContext.Update(newPairPerms);
        DbContext.Update(newPairAccess);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Callback to caller and pair of success. (In Future note that since we now do callback messages we can do this locally with no callback needed for instant change).
        await Clients.Caller.Callback_BulkChangeUnique(new(dto.User, dto.NewPerms, dto.NewAccess, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Callback_BulkChangeUnique(new(new(UserUID), dto.NewPerms, dto.NewAccess, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeUnique);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnGlobalPerm(SingleChangeGlobal dto)
    {
        // _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Caller must be the same as the target.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Perms must exist.
        if (await DbContext.GlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Change must be valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        // Otherwise, we correctly set the property and updated things.
        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        var pairsOfCaller = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var onlinePairsOfCaller = await GetOnlineUsers(pairsOfCaller).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfCaller.Keys;

        await Clients.Users([ ..onlinePairUids, UserUID]).Callback_SingleChangeGlobal(new(dto.User, dto.NewPerm, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeGlobal);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnPairPerm(SingleChangeUnique dto)
    {
        // _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Permissions must exist.
        if (await DbContext.PairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Ensure change is valid.
        if (!PropertyChanger.TrySetProperty(perms, dto.NewPerm.Key, dto.NewPerm.Value, out object? convertedValue))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.IncorrectDataType);

        DbContext.Update(perms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.User(UserUID).Callback_SingleChangeUnique(new(dto.User, dto.NewPerm, UpdateDir.Own, dto.Enactor)).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Callback_SingleChangeUnique(new(new(UserUID), dto.NewPerm, UpdateDir.Other, dto.Enactor)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeUnique);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserChangeOwnPairPermAccess(SingleChangeAccess dto)
    {
        //_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Permissions must exist.
        if (await DbContext.PairAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
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

    // should only ever be done by automatic timer expiration. If people get really
    // hacky with it and break things client side, this can easily be embedded into
    // a cleanup task server-side, (but also, it is necessary for safeword maybe?)
    [Authorize(Policy = "Identified")]
    public async Task<HubResponse<HardcoreStatus>> UserHardcoreAttributeExpired(HardcoreAttributeExpired dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));
        // Hardcore State must exist.
        if (await DbContext.HardcoreState.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } hcState)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new GagspeakAPI.Data.Permissions.HardcoreStatus());

        // Attribute MUST be enabled, because we should only disable.
        if (!hcState.IsEnabled(dto.Attribute))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidDataState, new GagspeakAPI.Data.Permissions.HardcoreStatus());

        // Must be able to change (we can fake this with auto-unlock-service so dont worry)
        if (!hcState.CanChange(dto.Attribute, dto.Enactor.UID))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions, new GagspeakAPI.Data.Permissions.HardcoreStatus());

        switch (dto.Attribute)
        {
            case HcAttribute.Follow:
                hcState.LockedFollowing = string.Empty;
                break;

            case HcAttribute.EmoteState:
                hcState.LockedEmoteState = string.Empty;
                hcState.EmoteExpireTime = DateTimeOffset.MinValue;
                hcState.EmoteId = 0;
                hcState.EmoteCyclePose = 0;
                break;

            case HcAttribute.Confinement:
                hcState.IndoorConfinement = string.Empty;
                hcState.ConfinementTimer = DateTimeOffset.MinValue;
                hcState.ConfinedWorld = 0;
                hcState.ConfinedCity = 0;
                hcState.ConfinedWard = 0;
                hcState.ConfinedPlaceId = 0;
                hcState.ConfinedInApartment = false;
                hcState.ConfinedInSubdivision = false;
                break;

            case HcAttribute.Imprisonment:
                hcState.Imprisonment = string.Empty;
                hcState.ImprisonmentTimer = DateTimeOffset.MinValue;
                hcState.ImprisonedTerritory = 0;
                hcState.ImprisonedPosX = 0;
                hcState.ImprisonedPosY = 0;
                hcState.ImprisonedPosZ = 0;
                hcState.ImprisonedRadius = 0;
                break;

            case HcAttribute.HiddenChatBox:
                hcState.ChatBoxesHidden = string.Empty;
                hcState.ChatBoxesHiddenTimer = DateTimeOffset.MinValue;
                break;

            case HcAttribute.HiddenChatInput:
                hcState.ChatInputHidden = string.Empty;
                hcState.ChatInputHiddenTimer = DateTimeOffset.MinValue;
                break;

            case HcAttribute.BlockedChatInput:
                hcState.ChatInputBlocked = string.Empty;
                hcState.ChatInputBlockedTimer = DateTimeOffset.MinValue;
                break;

            default:
                return HubResponseBuilder.AwDangIt(GagSpeakApiEc.BadUpdateKind, new GagspeakAPI.Data.Permissions.HardcoreStatus());

        }

        DbContext.Update(hcState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var newData = hcState.ToApi();

        var pairsOfCaller = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var onlinePairsOfCaller = await GetOnlineUsers(pairsOfCaller).ConfigureAwait(false);
        IEnumerable<string> onlinePairUids = onlinePairsOfCaller.Keys;

        await Clients.Users(onlinePairUids).Callback_StateChangeHardcore(new(new(UserUID), newData, dto.Attribute, dto.Enactor)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterPermissionChangeHardcore);
        return HubResponseBuilder.Yippee(newData);
    }
}

