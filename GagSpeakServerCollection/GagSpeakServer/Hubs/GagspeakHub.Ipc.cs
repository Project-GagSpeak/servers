using GagspeakAPI;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary> 
/// Handles IPC (Inter-Player Communication) between two paired users.
/// </summary>
public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushMoodlesFull(PushMoodlesFull dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_MoodleDataUpdated(new(new(UserUID), dto.NewData)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodleTransferFull);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushMoodlesSM(PushMoodlesSM dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_MoodleSMUpdated(new(new(UserUID), dto.DataString, dto.DataInfo)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodleTransferSM);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushMoodlesStatuses(PushMoodlesStatuses dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_MoodleStatusesUpdate(new(new(UserUID), dto.Statuses)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodleTransferStatus);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushMoodlesPresets(PushMoodlesPresets dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_MoodlePresetsUpdate(new(new(UserUID), dto.Presets)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodleTransferPreset);
		return HubResponseBuilder.Yippee();
	}

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushStatusModified(PushStatusModified dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_MoodleStatusModified(new(new(UserUID), dto.Status, dto.Deleted)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushPresetModified(PushPresetModified dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_MoodlePresetModified(new(new(UserUID), dto.Preset, dto.Deleted)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterMoodleTransferPreset);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyMoodlesByGuid(ApplyMoodleId dto)
	{
		// Must be paired.
		if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } perms)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

		// Must have permission.
		if ((perms.MoodleAccess & MoodleAccess.AllowOwn) == MoodleAccess.None)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// Apply it to them.
		await Clients.User(dto.User.UID).Callback_ApplyMoodlesByGuid(new(new(UserUID), dto.Ids, dto.IsPresets, dto.LockIds)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedId);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyMoodlesByStatus(ApplyMoodleStatus dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        // Must have permission.
        if (!pairPerms.MoodleAccess.HasAny(MoodleAccess.AllowOther))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// If someone applies this via Moodles, Moodles does its own internal check before sending.
		// If sent from GagSpeak, it also does it's own check.
		// So there is no reason to check permissions, we can do one on the recieving client as a failsafe, but not much reason to do so here.
		await Clients.User(dto.User.UID).Callback_ApplyMoodlesByStatus(new(new(UserUID), dto.Statuses, dto.LockIds)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedStatus);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserRemoveMoodles(RemoveMoodleId dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        await Clients.User(dto.User.UID).Callback_RemoveMoodles(new(new(UserUID), dto.Ids)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesRemoved);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserClearMoodles(KinksterBase dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        // Must have permission.
        if (!pairPerms.MoodleAccess.HasAny(MoodleAccess.Clearing))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        await Clients.User(dto.User.UID).Callback_ClearMoodles(new(new(UserUID))).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesCleared);
        return HubResponseBuilder.Yippee();
	}

    [Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserShockKinkster(ShockCollarAction dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

		// Cannot shock self.
		if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

		// Must have valid globals.
		if (await DbContext.GlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } globals)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

		// Must be paired.
		if (await DbContext.PairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

		// Target must be in hardcore mode.
		if (!perms.InHardcore)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// ShareCode must exist.
		if (string.IsNullOrEmpty(globals.GlobalShockShareCode) && string.IsNullOrEmpty(perms.PiShockShareCode))
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidPassword);

		// Shock Target.
		await Clients.User(dto.User.UID).Callback_ShockInstruction(new(new(UserUID), dto.OpCode, dto.Intensity, dto.Duration)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterKinkstersShocked);
        return HubResponseBuilder.Yippee();
	}
}