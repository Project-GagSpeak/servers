using GagspeakAPI.Enums;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.User;
using GagspeakShared.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary> 
/// Handles IPC (Inter-Player Communication) between two paired users.
/// </summary>
public partial class GagspeakHub
{
    /// <summary> 
    /// Notifiy the recipient pair to apply the spesified Moodles to their status manager by their GUID. 
    /// <para>
    /// NOTICE: 
    /// This will NOT check for permission validation. 
    /// This does not include info about the moodles because it only knows the GUIDs.
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<bool> UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto)
    {
        // TODO: REMOVE THIS to prevent log spamming.
        _logger.LogCallInfo();

        // simply validate that they are an existing pair.
        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
            return false;
        }

        // ensure that the client caller has permission to apply the pairs own moodles.
        if (!pairPerms.PairCanApplyYourMoodlesToYou)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You do not have permission to apply moodles to this user!").ConfigureAwait(false);
            return false;
        }

        // construct a new dto with the client caller as the user.
        var newDto = new ApplyMoodlesByGuidDto(User: UserUID.ToUserDataFromUID(), Statuses: dto.Statuses, Type: dto.Type);

        // notify the recipient pair to apply the moodles.
        await Clients.User(dto.User.UID).Client_UserApplyMoodlesByGuid(newDto).ConfigureAwait(false);

        // increment the metrics.
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo);
        return true;
    }

    /// <summary>
    /// Notifiy the recipient pair to apply the spesified Moodles we provide from our own moodles list to their status manager.
    /// <para>
    /// NOTICE:
    /// This CAN check for permission validation, and will verify the moodles info fits 
    /// the allowed permissions. Will return false if it does not.
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<bool> UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto)
    {
        // TODO: REMOVE THIS to prevent log spamming.
        _logger.LogCallInfo();

        // simply validate that they are an existing pair.
        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null)
        {
             await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
             return false;
        }

        // TODO: This is likely going to get mixed up along the path, so ensure that it works during transfer.
        if (!pairPerms.PairCanApplyOwnMoodlesToYou)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You do not have permission to apply moodles to this user!").ConfigureAwait(false);
            return false;
        }

        var moodlesToApply = dto.Statuses;

        if (moodlesToApply.Any(m => m.Type == StatusType.Positive && !pairPerms.AllowPositiveStatusTypes))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "One of the Statuses have a positive type, which this pair does not allow!").ConfigureAwait(false);
            return false;
        }

        if (moodlesToApply.Any(m => m.Type == StatusType.Negative && !pairPerms.AllowNegativeStatusTypes))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "One of the Statuses have a negative type, which this pair does not allow!").ConfigureAwait(false);
            return false;
        }

        if (moodlesToApply.Any(m => m.Type == StatusType.Special && !pairPerms.AllowSpecialStatusTypes))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "One of the Statuses have a special type, which this pair does not allow!").ConfigureAwait(false);
            return false;
        }

        if (moodlesToApply.Any(m => m.NoExpire && !pairPerms.AllowPermanentMoodles))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "One of the Statuses is permanent, which this pair does not allow!").ConfigureAwait(false);
            return false;
        }

        // ensure to only check this condition as one to be flagged if it exceeds the time and is NOT marked as permanent.
        if (moodlesToApply.Any(m => new TimeSpan(m.Days, m.Hours, m.Minutes, m.Seconds) > pairPerms.MaxMoodleTime && m.NoExpire == false))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "One of the Statuses exceeds the max allowed time!").ConfigureAwait(false);
            return false;
        }

        // construct a new dto with the client caller as the user.
        var newDto = new ApplyMoodlesByStatusDto(User: UserUID.ToUserDataFromUID(), Statuses: moodlesToApply, Type: dto.Type);

        // notify the recipient pair to apply the moodles.
        await Clients.User(dto.User.UID).Client_UserApplyMoodlesByStatus(newDto).ConfigureAwait(false);

        // increment the metrics.
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo);
        return true;
    }

    /// <summary>
    /// Notifiy the recipient pair to remove the spesified Moodles from their status manager by their GUID.
    /// <para>
    /// Can be a single moodle, or a list of moodles. Invalid GUID's are simply ignored.
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<bool> UserRemoveMoodles(RemoveMoodlesDto dto)
    {
        // TODO: REMOVE THIS to prevent log spamming.
        _logger.LogCallInfo();

        // simply validate that they are an existing pair.
        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
            return false;
        }

        if (!pairPerms.AllowRemovingMoodles)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Permission to remove Moodles from this pair was not given!").ConfigureAwait(false);
            return false;
        }

        // construct a new dto with the client caller as the user.
        var newDto = new RemoveMoodlesDto(User: UserUID.ToUserDataFromUID(), Statuses: dto.Statuses);

        // notify the recipient pair to apply the moodles.
        await Clients.User(dto.User.UID).Client_UserRemoveMoodles(newDto).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Notifies the user to clear all active moodles from their status manager.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<bool> UserClearMoodles(UserDto dto)
    {
        // TODO: REMOVE THIS to prevent log spamming.
        _logger.LogCallInfo();

        // simply validate that they are an existing pair.
        var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPerms == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
            return false;
        }

        if (!pairPerms.AllowRemovingMoodles)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Permission to remove Moodles from this pair was not given!").ConfigureAwait(false);
            return false;
        }

        // construct a new dto with the client caller as the user.
        var newDto = new UserDto(User: UserUID.ToUserDataFromUID());

        // notify the recipient pair to apply the moodles.
        await Clients.User(dto.User.UID).Client_UserClearMoodles(newDto).ConfigureAwait(false);
        return true;
    }
}