using GagspeakAPI.Enums;
using GagspeakAPI.SignalR;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using GagspeakAPI.Data;

namespace GagspeakServer.Hubs;

/// <summary>
/// This file handles the update permissions for the client caller, or a client callers paired user.
/// <para>
/// Which is handled is determined by the function call name, and verification checks will be made.
/// </para>
/// </summary>
public partial class GagspeakHub
{
	// the input name should be the same as the client caller
	public async Task UserPushAllGlobalPerms(UserPairUpdateAllGlobalPermsDto dto)
	{
		_logger.LogCallInfo();
		if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Can only push a bulk permission update for your own global permissions.").ConfigureAwait(false);
			return;
		}

		// fetch the user global perm from the database.
		var globalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
		if (globalPerms == null) { await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false); return; }

		// update the permissions to the new values passed in.
		var newGlobalPerms = dto.GlobalPermissions.ToModelGlobalPerms(globalPerms);

		// update the database with the new permissions & save DB changes
		DbContext.Update(newGlobalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs of the client caller
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		await Clients.Caller.Client_UserUpdateAllGlobalPerms(new(new(UserUID), dto.Enactor, dto.GlobalPermissions, UpdateDir.Own)).ConfigureAwait(false);
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateAllGlobalPerms(new(new(UserUID), dto.Enactor, dto.GlobalPermissions, UpdateDir.Other)).ConfigureAwait(false);
	}

	// this dto pushes to ANOTHER INTENDED PAIR the permissions WE are updating for THEM. We should use callbacks accordingly.
	public async Task UserPushAllUniquePerms(UserPairUpdateAllUniqueDto dto)
	{
		_logger.LogCallInfo();
		if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't modify own perms on a UpdateOther call").ConfigureAwait(false);
			return;
		}

		// grab our pair permissions for this user.
		var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if (pairPerms == null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair Permission Not Found").ConfigureAwait(false);
			return;
		}
		// grab the pair permission access for this user.
		var pairAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if (pairAccess == null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission access not found").ConfigureAwait(false);
			return;
		}

		// Update the global permissions, pair permissions, and editAccess permissions with the new values.
		var newPairPerms = dto.UniquePerms.ToModelUserPairPerms(pairPerms);
		var newPairAccess = dto.UniqueAccessPerms.ToModelUserPairEditAccessPerms(pairAccess);

		// update the database with the new permissions & save DB changes
		DbContext.Update(newPairPerms);
		DbContext.Update(newPairAccess);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		await Clients.Caller.Client_UserUpdateAllUniquePerms(new(dto.User, dto.Enactor, dto.UniquePerms, dto.UniqueAccessPerms, UpdateDir.Own)).ConfigureAwait(false);
		await Clients.User(dto.User.UID).Client_UserUpdateAllUniquePerms(new(new(UserUID), dto.Enactor, dto.UniquePerms, dto.UniqueAccessPerms, UpdateDir.Other)).ConfigureAwait(false);
	}

	/// <summary> 
	/// Updates a global permission of the client caller to a new value.
	/// If successful, function will send update to client caller and their paired users.
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOwnGlobalPerm(UserGlobalPermChangeDto dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
		// Make sure the UserData within is for ourselves, since we called the [UpdateOwnGlobalPerm]
		if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't modify others perms when calling updateOwnPerm").ConfigureAwait(false); 
			return;
		}

		// fetch the user global perm from the database.
		var globalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
		if (globalPerms is null) 
		{ 
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false); 
			return; 
		}

		// Attempt to make the change.
		if(!globalPerms.UpdateGlobalPerm(dto.ChangedPermission, out string errorMsg))
		{
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, errorMsg).ConfigureAwait(false);
            return;
        }

		// Change was made, so update database.
		DbContext.Update(globalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs of the client caller
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		// callback to the client caller's pairs, letting them know that our permission was updated.
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdatePairPermsGlobal(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Other)).ConfigureAwait(false);
		// callback to the client caller to let them know that their permissions have been updated.
		await Clients.Caller.Client_UserUpdatePairPermsGlobal(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Own)).ConfigureAwait(false);
	}



	/// <summary> 
	/// Updates a global permission on one of the client caller's user pair to a new value.
	/// If successful, function will send update to client caller and their paired users.
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOtherGlobalPerm(UserGlobalPermChangeDto dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
		// Make sure the UserData within is for ourselves, since we called the [UpdateOwnGlobalPerm]
		if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't modify own perms on a UpdateOther call").ConfigureAwait(false);
			return;
		}

		// fetch the global permission table row belonging to the user in the Dto
		var globalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
		if (globalPerms is null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

        // Attempt to make the change.
        if (!globalPerms.UpdateGlobalPerm(dto.ChangedPermission, out string errorMsg))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, errorMsg).ConfigureAwait(false);
            return;
        }

        // update the database with the new global permission & save DB changes
        DbContext.Update(globalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs that the paired user we are updating has.
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		// debug the list of pairs of client
		/*foreach (var pair in pairsOfClient) _logger.LogMessage($"Pair: {pair.Key}");*/

		// send callback to all the paired users of the userpair we modified, informing them of the update (includes the client caller)
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdatePairPermsGlobal(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Other)).ConfigureAwait(false);
		
		// finally, send a callback to the client pair who just had their permissions updated.
		await Clients.User(dto.User.UID).Client_UserUpdatePairPermsGlobal(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Own)).ConfigureAwait(false);
	}


	/// <summary>
	/// Updates a pair permission of the client caller to a new value.
	/// If successful, function will send update to client caller and their paired user they are updating the permission for.
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOwnPairPerm(UserPairPermChangeDto dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

		// grab the pair permission, where the user is the client caller, and the other user is the one we are updating the pair permissions for.
		var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if(pairPerms is null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

		// store to see if change is a pause change
		bool prevPauseState = pairPerms.IsPaused;

        // Attempt to make the change to the permissions.
        if (!pairPerms.UpdatePairPerms(dto.ChangedPermission, out string errorMsg))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, errorMsg).ConfigureAwait(false);
            return;
        }

		DbContext.Update(pairPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// callback the updated info to the client caller as well so it can update properly.
		await Clients.User(UserUID).Client_UserUpdatePairPerms(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Own)).ConfigureAwait(false);
		// send a callback to the userpair we updated our permission for, so they get the updated info
		await Clients.User(dto.User.UID).Client_UserUpdatePairPerms(new(new(UserUID), dto.Enactor, dto.ChangedPermission, UpdateDir.Other)).ConfigureAwait(false);

		// check pause change
		if (!(pairPerms.IsPaused != prevPauseState))
			return;

		// we have performed a pause change, so need to make sure that we send online/offline respectively base on update.
		_logger.LogMessage("Pause change detected, checking if both users are online to send online/offline updates.");
		// grab the other players pair perms for you
		var otherPairData = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
		if (otherPairData is not null && !otherPairData.IsPaused)
		{
			// only perform the following if they are online.
			var otherCharaIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
			if (UserCharaIdent is null || otherCharaIdent is null)
				return;

			// if the new value is true (we are pausing them) and they have not paused us, we must send offline for both.
			if ((bool)dto.ChangedPermission.Value)
			{
				await Clients.User(UserUID).Client_UserSendOffline(new(new(dto.User.UID))).ConfigureAwait(false);
				await Clients.User(dto.User.UID).Client_UserSendOffline(new(new(UserUID))).ConfigureAwait(false);
			}
			// Otherwise, its false, and they dont have us paused, so send online to both.
			else
			{
				await Clients.User(UserUID).Client_UserSendOnline(new(new(dto.User.UID), otherCharaIdent)).ConfigureAwait(false);
				await Clients.User(dto.User.UID).Client_UserSendOnline(new(new(UserUID), UserCharaIdent)).ConfigureAwait(false);
			}
		}
	}


	/// <summary>
	/// Updates a pair permission of one of the client caller's paired users to a new value.
	/// If successful, function will send update to the paired user being updated, and the client caller (unless all paired users are needed?)
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOtherPairPerm(UserPairPermChangeDto dto)
	{
		// no way to verify if we are using it properly, so just make the assumption that we are.
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

		// grab the pair permission row belonging to the paired user so we can modify it.
		var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);
		if (pairPerms is null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

        // Attempt to make the change to the permissions.
        if (!pairPerms.UpdatePairPerms(dto.ChangedPermission, out string errorMsg))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, errorMsg).ConfigureAwait(false);
            return;
        }

        DbContext.Update(pairPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// inform the userpair we modified to update their own permissions
		await Clients.User(dto.User.UID).Client_UserUpdatePairPerms(new(new(UserUID), dto.Enactor, dto.ChangedPermission, UpdateDir.Own)).ConfigureAwait(false);
		// inform the client caller to update the modified userpairs permission
		await Clients.Caller.Client_UserUpdatePairPerms(new(dto.User, dto.Enactor, dto.ChangedPermission, UpdateDir.Other)).ConfigureAwait(false);
	}

	/// <summary>
	/// This will update the edit access permission for the client caller's paired user, indicating the access perms they set for someone else.
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOwnPairPermAccess(UserPairAccessChangeDto dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
		// grab the edit access permission
		var pairAccess = await DbContext.ClientPairPermissionAccess.SingleAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);

        // Attempt to make the change to the permissions.
        if (!pairAccess.UpdatePairPermsAccess(dto.ChangedAccessPermission, out string errorMsg))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, errorMsg).ConfigureAwait(false);
            return;
        }

        DbContext.Update(pairAccess);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// send a callback to the userpair we updated our permission for, so they get the updated info (we update the user so that when the pair receives it they know who to update this for)
		await Clients.User(dto.User.UID).Client_UserUpdatePairPermAccess(new(new(UserUID), dto.Enactor, dto.ChangedAccessPermission, UpdateDir.Other)).ConfigureAwait(false);
		// callback the updated info to the client caller as well so it can update properly.
		await Clients.Caller.Client_UserUpdatePairPermAccess(new(dto.User, dto.Enactor, dto.ChangedAccessPermission, UpdateDir.Own)).ConfigureAwait(false);
	}
}
