using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
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
	public async Task<HubResponse> UserPushIpcFull(PushIpcDataFull dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_SetKinksterIpcFull(new(new(UserUID), new(UserUID), dto.NewData)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterStateTransfersIpcFull);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushIpcStatusManager(PushIpcStatusManager dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_SetKinksterIpcStatusManager(new(new(UserUID), new(UserUID), dto.DataString, dto.DataInfo)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterStateTransfersIpcSM);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushIpcStatuses(PushIpcStatuses dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_SetKinksterIpcStatuses(new(new(UserUID), new(UserUID), dto.Statuses)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterStateTransfersIpcStatus);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserPushIpcPresets(PushIpcPresets dto)
	{
		var recipientUids = dto.Recipients.Select(r => r.UID);
		await Clients.Users(recipientUids).Callback_SetKinksterIpcPresets(new(new(UserUID), new(UserUID), dto.Presets)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterStateTransfersIpcPreset);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyMoodlesByGuid(MoodlesApplierById dto)
	{
		// Must be paired.
		if (await DbContext.ClientPairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

		// Must have permission.
		if ((pairPerms.MoodlePerms & MoodlePerms.PairCanApplyYourMoodlesToYou) == MoodlePerms.None)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// Apply it to them.
		await Clients.User(dto.User.UID).Callback_ApplyMoodlesByGuid(new(new(UserUID), dto.Ids, dto.Type)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedId);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyMoodlesByStatus(MoodlesApplierByStatus dto)
	{
        // Must be paired.
        if (await DbContext.ClientPairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Must have permission.
        if ((pairPerms.MoodlePerms & MoodlePerms.PairCanApplyTheirMoodlesToYou) == MoodlePerms.None)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// Validate permission to apply based on the moodlePerms.
        var moodlesToApply = dto.Statuses;

		if (moodlesToApply.Any(m => m.Type is StatusType.Positive && (pairPerms.MoodlePerms & MoodlePerms.PositiveStatusTypes) == MoodlePerms.None))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "One of the Statuses have a positive type, which this pair does not allow!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}
		else if (moodlesToApply.Any(m => m.Type is StatusType.Negative && (pairPerms.MoodlePerms & MoodlePerms.NegativeStatusTypes) == MoodlePerms.None))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "One of the Statuses have a negative type, which this pair does not allow!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}
		else if (moodlesToApply.Any(m => m.Type is StatusType.Special && (pairPerms.MoodlePerms & MoodlePerms.SpecialStatusTypes) == MoodlePerms.None))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "One of the Statuses have a special type, which this pair does not allow!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}
		else if (moodlesToApply.Any(m => m.NoExpire && (pairPerms.MoodlePerms & MoodlePerms.PermanentMoodles) == MoodlePerms.None))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "One of the Statuses is permanent, which this pair does not allow!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}
		else if (moodlesToApply.Any(m => new TimeSpan(m.Days, m.Hours, m.Minutes, m.Seconds) > pairPerms.MaxMoodleTime && m.NoExpire == false))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "One of the Statuses exceeds the max allowed time!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}

		// Apply them.
		await Clients.User(dto.User.UID).Callback_ApplyMoodlesByStatus(new(new(UserUID), moodlesToApply, dto.Type)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedStatus);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserRemoveMoodles(MoodlesRemoval dto)
	{
        // Must be paired.
        if (await DbContext.ClientPairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Must have permission.
        if ((pairPerms.MoodlePerms & MoodlePerms.RemovingMoodles) == MoodlePerms.None)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// Remove the moodle.
        await Clients.User(dto.User.UID).Callback_RemoveMoodles(new(new(UserUID), dto.StatusIds)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesRemoved);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserClearMoodles(KinksterBase dto)
	{
        // Must be paired.
        if (await DbContext.ClientPairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        // Must have permission.
        if ((pairPerms.MoodlePerms & MoodlePerms.ClearingMoodles) == MoodlePerms.None)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        // Clear them.
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
		if (await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false) is not { } globals)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

		// Must be paired.
		if (await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false) is not { } perms)
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