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
    public async Task<HubResponse> UserPushLociData(PushLociData dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_LociDataUpdated(new(new(UserUID), dto.Data)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterMoodleTransferFull);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushLociStatuses(PushLociStatuses dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_LociStatusesUpdate(new(new(UserUID), dto.Statuses)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterMoodleTransferStatus);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushLociPresets(PushLociPresets dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_LociPresetsUpdate(new(new(UserUID), dto.Presets)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterMoodleTransferPreset);
		return HubResponseBuilder.Yippee();
	}

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushStatusModified(PushStatusModified dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_LociStatusModified(new(new(UserUID), dto.Status, dto.Deleted)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
    public async Task<HubResponse> UserPushPresetModified(PushPresetModified dto)
    {
        var recipientUids = dto.Recipients.Select(r => r.UID);
        await Clients.Users(recipientUids).Callback_LociPresetModified(new(new(UserUID), dto.Preset, dto.Deleted)).ConfigureAwait(false);
        _metrics.IncCounter(MetricsAPI.CounterMoodleTransferPreset);
        return HubResponseBuilder.Yippee();
    }

    [Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyLociData(ApplyLociDataById dto)
	{
		// Must be paired.
		if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } perms)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

		// Must have permission.
		if ((perms.LociAccess & LociAccess.AllowOwn) == LociAccess.None)
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		// Apply it to them.
		await Clients.User(dto.User.UID).Callback_LociApplyDataById(new(new(UserUID), dto.Ids, dto.IsPresets, dto.LockIds)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedId);
		return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserApplyLociStatusTuples(ApplyLociStatus dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        // Must have permission.
        if (!pairPerms.LociAccess.HasAny(LociAccess.AllowOther))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

		await Clients.User(dto.User.UID).Callback_LociApplyStatus(new(new(UserUID), dto.Statuses, dto.LockIds)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesAppliedStatus);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserRemoveLociData(RemoveLociData dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);

        await Clients.User(dto.User.UID).Callback_LociRemoveData(new(new(UserUID), dto.Ids)).ConfigureAwait(false);
		_metrics.IncCounter(MetricsAPI.CounterMoodlesRemoved);
        return HubResponseBuilder.Yippee();
	}

	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserClearLociData(KinksterBase dto)
	{
        if (await DbContext.PairPermissions.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } pairPerms)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
        // Must have permission.
        if (!pairPerms.LociAccess.HasAny(LociAccess.Clearing))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);

        await Clients.User(dto.User.UID).Callback_LociClearData(new(new(UserUID))).ConfigureAwait(false);
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
		if (await DbContext.PairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false) is not { } perms)
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