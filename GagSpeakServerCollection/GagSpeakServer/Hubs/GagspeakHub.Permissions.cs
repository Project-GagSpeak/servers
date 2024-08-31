using GagspeakAPI.Data.Enum;
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
	/// <summary>
	/// ONLY CALLED UPON WHEN ADDING A NEW USER. SHOULD NOT BE CALLED UPON FOR ANY OTHER REASON.
	/// </summary>
	public async Task UserPushAllPerms(UserPairUpdateAllPermsDto dto)
	{
		_logger.LogCallInfo();
		// Throw exception if we try to update ourselves
		if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal))
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't modify own perms on a UpdateOther call").ConfigureAwait(false);
			return;
		}

		// grab our global permissions
		var globalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);

		// grab our pair permissions for this user.
		var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if(pairPerms == null) throw new InvalidOperationException("Pair permission not found");

		// grab the pair permission access for this user.
		var pairAccess = await DbContext.ClientPairPermissionAccess.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if(pairAccess == null) throw new InvalidOperationException("Pair permission access not found");

		// We have reached here, which means we can bulk update our permissions for this pair.

		// Update the global permissions, pair permissions, and editAccess permissions with the new values.
		globalPerms = dto.GlobalPermissions.ToModelGlobalPerms();
		pairPerms = dto.PairPermissions.ToModelUserPairPerms();
		pairAccess = dto.EditAccessPermissions.ToModelUserPairEditAccessPerms();

		// update the database with the new permissions & save DB changes
		DbContext.Update(globalPerms);
		DbContext.Update(pairPerms);
		DbContext.Update(pairAccess);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// callback to the user we pushed the permissions for that we have our permissions updated
		await Clients.User(dto.User.UID).Client_UserUpdateOtherAllPairPerms(new(dto.User, dto.GlobalPermissions, dto.PairPermissions, dto.EditAccessPermissions, false)).ConfigureAwait(false);
		// callback to the client caller that we have updated our permissions
		await Clients.Caller.Client_UserUpdateOtherAllPairPerms(new(dto.User, dto.GlobalPermissions, dto.PairPermissions, dto.EditAccessPermissions, true)).ConfigureAwait(false);
	}

	// the input name should be the same as the client caller
	public async Task UserPushAllGlobalPerms(UserAllGlobalPermChangeDto dto)
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
		globalPerms = dto.GlobalPermissions.ToModelGlobalPerms();

		// update the database with the new permissions & save DB changes
		DbContext.Update(globalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs of the client caller
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		await Clients.Caller.Client_UserUpdateSelfAllGlobalPerms(new(UserUID.ToUserDataFromUID(), dto.GlobalPermissions)).ConfigureAwait(false);
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherAllGlobalPerms(new(UserUID.ToUserDataFromUID(), dto.GlobalPermissions)).ConfigureAwait(false);
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
		pairPerms = dto.UniquePerms.ToModelUserPairPerms();
		pairAccess = dto.UniqueAccessPerms.ToModelUserPairEditAccessPerms();

		// update the database with the new permissions & save DB changes
		DbContext.Update(pairPerms);
		DbContext.Update(pairAccess);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		await Clients.Caller.Client_UserUpdateSelfAllUniquePerms(new(dto.User, dto.UniquePerms, dto.UniqueAccessPerms)).ConfigureAwait(false);
		await Clients.User(dto.User.UID).Client_UserUpdateOtherAllUniquePerms(new(UserUID.ToUserDataFromUID(), dto.UniquePerms, dto.UniqueAccessPerms)).ConfigureAwait(false);
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
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't modify others perms when calling updateOwnPerm").ConfigureAwait(false); return;
		}

		// fetch the user global perm from the database.
		var globalPerms = await DbContext.UserGlobalPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
		if (globalPerms == null) { await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false); return; }

		// establish the key-value pair from the Dto so we know what is changing.
		string propertyName = dto.ChangedPermission.Key;
		object newValue = dto.ChangedPermission.Value;
		// Get the PropertyInfo object for the property with the matching name in globalPerms
		PropertyInfo propertyInfo = typeof(GagspeakShared.Models.UserGlobalPermissions).GetProperty(propertyName);
		if (propertyInfo != null)
		{
			// Catches Boolean & String recognition
			if (propertyInfo.PropertyType == newValue.GetType())
			{
				// [YES THIS IS WHERE IT UPDATES THE ACTUAL GLOBALPERMS OBJECT]
				propertyInfo.SetValue(globalPerms, newValue);
			}
			// timespan recognition. (these are converted to Uint64 for Dto's instead of TimeSpan)
			else if (newValue.GetType() == typeof(UInt64) && propertyInfo.PropertyType == typeof(TimeSpan))
			{
				propertyInfo.SetValue(globalPerms, TimeSpan.FromTicks((long)(ulong)newValue));
			}
			// char recognition. (these are converted to byte for Dto's instead of char)
			else if (newValue.GetType() == typeof(byte) && propertyInfo.PropertyType == typeof(char))
			{
				propertyInfo.SetValue(globalPerms, Convert.ToChar(newValue));
			}
			else
			{
				await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type miss-match on requested update").ConfigureAwait(false);
				return;
			}
		}
		else
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
			return;
		}

		// update the database with the new global permission & save DB changes
		DbContext.Update(globalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs of the client caller
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		// callback to the client caller's pairs, letting them know that our permission was updated.
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherPairPermsGlobal(dto).ConfigureAwait(false);

		// callback to the client caller to let them know that their permissions have been updated.
		await Clients.Caller.Client_UserUpdateSelfPairPermsGlobal(dto).ConfigureAwait(false);
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
		if (globalPerms == null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

		// establish the keyvalue pair from the Dto so we know what is changing.
		string propertyName = dto.ChangedPermission.Key;
		object newValue = dto.ChangedPermission.Value;
		PropertyInfo propertyInfo = typeof(GagspeakShared.Models.UserGlobalPermissions).GetProperty(propertyName);
		if (propertyInfo != null)
		{
			// Boolean & String recognition
			if (propertyInfo.PropertyType == newValue.GetType())
			{
				// [YES THIS IS WHERE IT UPDATES THE ACTUAL GLOBALPERMS OBJECT]
				propertyInfo.SetValue(globalPerms, newValue);
			}
			// timespan recognition. (these are converted to Uint64 for Dto's instead of TimeSpan)
			else if (newValue.GetType() == typeof(UInt64) && propertyInfo.PropertyType == typeof(TimeSpan))
			{
				propertyInfo.SetValue(globalPerms, TimeSpan.FromTicks((long)(ulong)newValue));
			}
			// char recognition. (these are converted to byte for Dto's instead of char)
			else if (newValue.GetType() == typeof(byte) && propertyInfo.PropertyType == typeof(char))
			{
				propertyInfo.SetValue(globalPerms, Convert.ToChar(newValue));
			}
			else
			{
				await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type miss-match on requested update").ConfigureAwait(false);
				return;
			}
		}
		else
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
			return;
		}

		// update the database with the new global permission & save DB changes
		DbContext.Update(globalPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// grab the user pairs that the paired user we are updating has.
		List<string> allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);

		// now get all the online users out of that batch.
		var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

		// debug the list of pairs of client
		foreach (var pair in pairsOfClient)
		{
			_logger.LogMessage($"Pair: {pair.Key}");
		}

		// send callback to all the paired users of the userpair we modified, informing them of the update (includes the client caller)
		await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherPairPermsGlobal(dto).ConfigureAwait(false);
		
		// finally, send a callback to the client pair who just had their permissions updated.
		await Clients.User(dto.User.UID).Client_UserUpdateSelfPairPermsGlobal(dto).ConfigureAwait(false);
	}



	/// <summary>
	/// Updates a pair permission of the client caller to a new value.
	/// If successful, function will send update to client caller and their paired user they are updating the permission for.
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOwnPairPerm(UserPairPermChangeDto dto)
	{
		// no way to verify if we are using it properly, so just make the assumption that we are.
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));

		// grab the pair permission, where the user is the client caller, and the other user is the one we are updating the pair permissions for.
		var pairPerms = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);
		if(pairPerms == null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

		// store to see if change is a pause change
		bool pauseChange = false;

		// establish the keyvalue pair from the Dto so we know what is changing.
		string propertyName = dto.ChangedPermission.Key;
		object newValue = dto.ChangedPermission.Value;
		PropertyInfo propertyInfo = typeof(ClientPairPermissions).GetProperty(propertyName);
		if (propertyInfo != null)
		{
			// Standard boolean & string recognition
			if (propertyInfo.PropertyType == newValue.GetType())
			{
				// before making change, see if the property name is "IsPaused", and if its new value is different from the current value.
				if (string.Equals(propertyName, "IsPaused", StringComparison.Ordinal) && pairPerms.IsPaused != (bool)newValue)
				{
					pauseChange = true;
				}

				propertyInfo.SetValue(pairPerms, newValue);
			}
			// timespan recognition. (these are converted to Uint64 for Dto's instead of TimeSpan)
			else if (newValue.GetType() == typeof(UInt64) && propertyInfo.PropertyType == typeof(TimeSpan))
			{
				propertyInfo.SetValue(pairPerms, TimeSpan.FromTicks((long)(ulong)newValue));
			}
			// char recognition. (these are converted to byte for Dto's instead of char)
			else if (newValue.GetType() == typeof(byte) && propertyInfo.PropertyType == typeof(char))
			{
				propertyInfo.SetValue(pairPerms, Convert.ToChar(newValue));
			}
			else
			{
				// debug the two property types so we know why it happened
				_logger.LogMessage($"PropertyType: {propertyInfo.PropertyType}, NewValueType: {newValue.GetType()}");
				await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type miss-match on requested update").ConfigureAwait(false);
				return;
			}
		}
		else
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
			return;
		}

		DbContext.Update(pairPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);
		// fetch our user
		User? user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
		if (user == null) throw new Exception("User not found");

		// callback the updated info to the client caller as well so it can update properly.
		await Clients.User(UserUID).Client_UserUpdateSelfPairPerms(dto).ConfigureAwait(false);
		// send a callback to the userpair we updated our permission for, so they get the updated info
		await Clients.User(dto.User.UID).Client_UserUpdateOtherPairPerms(new UserPairPermChangeDto(new UserData(user.UID, user.Alias), dto.ChangedPermission)).ConfigureAwait(false);

		// grab the other players pair perms for you
		var pairData = await DbContext.ClientPairPermissions.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID && u.OtherUserUID == UserUID).ConfigureAwait(false);

		// check to 
		if (pauseChange && pairData != null && !pairData.IsPaused)
		{
			_logger.LogMessage("Pause change detected, checking if both users are online to send online/offline updates.");
			// obtain the other character identifier
			var otherCharaIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);

			// if our user is null, or the other user is null, we are not both online, so dont do this.
			if (UserCharaIdent == null || otherCharaIdent == null) return;

			if ((bool)newValue)
			{
				await Clients.User(UserUID).Client_UserSendOffline(new(new(dto.User.UID))).ConfigureAwait(false);
				await Clients.User(dto.User.UID).Client_UserSendOffline(new(new(UserUID))).ConfigureAwait(false);
			}
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
		if (pairPerms == null)
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
			return;
		}

		// establish the keyvalue pair from the Dto so we know what is changing.
		string propertyName = dto.ChangedPermission.Key;
		object newValue = dto.ChangedPermission.Value;
		PropertyInfo propertyInfo = typeof(ClientPairPermissions).GetProperty(propertyName);
		if (propertyInfo != null)
		{
			// Catches boolean & string recognition
			if (propertyInfo.PropertyType == newValue.GetType())
			{
				// [YES THIS IS WHERE IT UPDATES THE ACTUAL PAIRPERMS OBJECT]
				propertyInfo.SetValue(pairPerms, newValue);
			}
			// timespan recognition. (these are converted to Uint64 for Dto's instead of TimeSpan)
			else if (newValue.GetType() == typeof(UInt64) && propertyInfo.PropertyType == typeof(TimeSpan))
			{
				propertyInfo.SetValue(pairPerms, TimeSpan.FromTicks((long)(ulong)newValue));
			}
			// char recognition. (these are converted to byte for Dto's instead of char)
			else if (newValue.GetType() == typeof(byte) && propertyInfo.PropertyType == typeof(char))
			{
				propertyInfo.SetValue(pairPerms, Convert.ToChar(newValue));
			}
			else
			{
				await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type miss-match on requested update").ConfigureAwait(false);
				return;
			}
		}
		else
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
			return;
		}

		DbContext.Update(pairPerms);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// inform the userpair we modified to update their own permissions
		await Clients.User(dto.User.UID).Client_UserUpdateSelfPairPerms(new(new UserData(UserUID), dto.ChangedPermission)).ConfigureAwait(false);
		// inform the client caller to update the modified userpairs permission
		await Clients.Caller.Client_UserUpdateOtherPairPerms(dto).ConfigureAwait(false);
	}

	/// <summary>
	/// This will update the edit access permission for a paired user.
	/// <para>
	/// This should ONLY EVER work if the user in the Dto to change is equal to the UserUID claim of the caller. 
	/// The only person allowed to edit access is ones self.
	/// </para>
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task UserUpdateOwnPairPermAccess(UserPairAccessChangeDto dto)
	{
		_logger.LogCallInfo(GagspeakHubLogger.Args(dto));
		// grab the edit access permission
		var pairAccess = await DbContext.ClientPairPermissionAccess.SingleAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);

		// establish the key-value pair from the Dto so we know what is changing.
		string propertyName = dto.ChangedAccessPermission.Key;
		object newValue = dto.ChangedAccessPermission.Value;
		PropertyInfo propertyInfo = typeof(ClientPairPermissionAccess).GetProperty(propertyName);
		if (propertyInfo != null)
		{
			// Ensure the type of the newValue matches the property type
			if (propertyInfo.PropertyType == newValue.GetType())
			{
				try
				{
					// log the debug output of the types
					_logger.LogMessage($"PropertyType: {propertyInfo.PropertyType}, NewValueType: {newValue.GetType()}");
					// Set the new value for the property
					propertyInfo.SetValue(pairAccess, newValue);
				}
				catch(TargetException ex)
				{
					_logger.LogMessage($"TargetException setting value: {ex.Message}");
				}
				catch(ArgumentException ex)
				{
					_logger.LogMessage($"ArgumentException setting value: {ex.Message}");
				}
				catch(MethodAccessException ex)
				{
					_logger.LogMessage($"MethodAccessException setting value: {ex.Message}");
				}
				catch(Exception ex)
				{
					_logger.LogMessage($"Error setting value: {ex.Message}");
				}
			}
			else
			{
				await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type miss-match on requested update").ConfigureAwait(false);
				return;
			}
		}
		else
		{
			await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
			return;
		}

		DbContext.Update(pairAccess);
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// fetch our user
		User? user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);

		// send a callback to the userpair we updated our permission for, so they get the updated info (we update the user so that when the pair receives it they know who to update this for)
		await Clients.User(dto.User.UID).Client_UserUpdateOtherPairPermAccess(new UserPairAccessChangeDto(new GagspeakAPI.Data.UserData(user!.UID, user.Alias), dto.ChangedAccessPermission)).ConfigureAwait(false);
		// callback the updated info to the client caller as well so it can update properly.
		await Clients.Caller.Client_UserUpdateSelfPairPermAccess(dto).ConfigureAwait(false);
	}
}
