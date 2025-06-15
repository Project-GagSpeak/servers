using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Models;
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
     /// </summary>
     [Authorize(Policy = "Identified")]
     public async Task<HubResponse> UserApplyMoodlesByGuid(MoodlesApplierById dto)
     {
        // simply validate that they are an existing pair.
        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
          if (pairPerms is null)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
          }

          // ensure that the client caller has permission to apply the pairs own moodles.
          if ((pairPerms.MoodlePerms & MoodlePerms.PairCanApplyYourMoodlesToYou) == MoodlePerms.None)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "You do not have permission to apply moodles to this user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}

		// construct a new dto with the client caller as the user.
		MoodlesApplierById newDto = new(UserUID.ToUserDataFromUID(), dto.Ids, Type: dto.Type);

          // notify the recipient pair to apply the moodles.
          await Clients.User(dto.User.UID).Callback_ApplyMoodlesByGuid(newDto).ConfigureAwait(false);
          return HubResponseBuilder.Yippee();
	}

     /// <summary>
     /// Notifiy the recipient pair to apply the spesified Moodles we provide from our own moodles list to their status manager.
     /// </summary>
     [Authorize(Policy = "Identified")]
     public async Task<HubResponse> UserApplyMoodlesByStatus(MoodlesApplierByStatus dto)
     {
        // simply validate that they are an existing pair.
        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
          if (pairPerms is null)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
		}
          if ((pairPerms.MoodlePerms & MoodlePerms.PairCanApplyTheirMoodlesToYou) == MoodlePerms.None)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "You do not have permission to apply moodles to this user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}

          IEnumerable<MoodlesStatusInfo> moodlesToApply = dto.Statuses;

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

		// construct a new dto with the client caller as the user.
		MoodlesApplierByStatus newDto = new(UserUID.ToUserDataFromUID(), moodlesToApply, dto.Type);
          // notify the recipient pair to apply the moodles.
          await Clients.User(dto.User.UID).Callback_ApplyMoodlesByStatus(newDto).ConfigureAwait(false);
          return HubResponseBuilder.Yippee();
	}

     /// <summary>
     /// Notifiy the recipient pair to remove the spesified Moodles from their status manager by their GUID.
     /// </summary>
     [Authorize(Policy = "Identified")]
     public async Task<HubResponse> UserRemoveMoodles(MoodlesRemoval dto)
     {
        // simply validate that they are an existing pair.
        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
          if (pairPerms is null)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
		}
          else if ((pairPerms.MoodlePerms & MoodlePerms.RemovingMoodles) == MoodlePerms.None)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Permission to remove Moodles from this pair was not given!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}

          // construct a new dto with the client caller as the user.
          MoodlesRemoval newDto = new(UserUID.ToUserDataFromUID(), dto.StatusIds);
          // notify the recipient pair to apply the moodles.
          await Clients.User(dto.User.UID).Callback_RemoveMoodles(newDto).ConfigureAwait(false);
          return HubResponseBuilder.Yippee();
	}

     /// <summary> Notifies the user to clear all active moodles from their status manager. </summary>
     [Authorize(Policy = "Identified")]
     public async Task<HubResponse> UserClearMoodles(KinksterBase dto)
     {
        // simply validate that they are an existing pair.
        ClientPairPermissions pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
          if (pairPerms is null)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Cannot apply moodles to a non-paired user!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
		}

          if ((pairPerms.MoodlePerms & MoodlePerms.RemovingMoodles) == MoodlePerms.None)
          {
               await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Permission to remove Moodles from this pair was not given!").ConfigureAwait(false);
               return HubResponseBuilder.AwDangIt(GagSpeakApiEc.LackingPermissions);
		}

          // notify the recipient pair to apply the moodles.
          await Clients.User(dto.User.UID).Callback_ClearMoodles(new(UserUID.ToUserDataFromUID())).ConfigureAwait(false);
          return HubResponseBuilder.Yippee();
	}

	/// <summary>
	/// Sends an action to a paired users Shock Collar.
	/// Must verify they are in hardcore mode to proceed.
	/// </summary>
	[Authorize(Policy = "Identified")]
	public async Task<HubResponse> UserShockKinkster(ShockCollarAction dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

		if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "Cannot send a shock request to yourself! (yet?)").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
		}

        // make sure they are added as a pair of the client caller.
        UserGlobalPermissions userPairGlobalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        ClientPairPermissions userPairPermsForCaller = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
		if (userPairPermsForCaller is null || userPairGlobalPerms is null)
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "User is not paired with you").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPaired);
		}

		// ensure that this user is in hardcore mode with you.
		if (!userPairPermsForCaller.InHardcore ||
		(string.IsNullOrEmpty(userPairGlobalPerms.GlobalShockShareCode) && string.IsNullOrEmpty(userPairPermsForCaller.PiShockShareCode)))
		{
			await Clients.Caller.Callback_ServerMessage(MessageSeverity.Error, "User is not in hardcore mode with you, or " +
			    "doesn't have any shock collars configured!").ConfigureAwait(false);
			return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
		}

		// otherwise, it is valid, so attempt to send the shock instruction to the user.
		await Clients.User(dto.User.UID).Callback_ShockInstruction(new(UserUID.ToUserDataFromUID(), dto.OpCode, dto.Intensity, dto.Duration)).ConfigureAwait(false);
          return HubResponseBuilder.Yippee();
	}
}