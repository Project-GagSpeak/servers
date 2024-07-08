using Gagspeak.API.Data.Enum;
using Gagspeak.API.SignalR;
using GagSpeak.API.Data.Permissions;
using GagSpeak.API.Dto.Permissions;
using GagspeakServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GagspeakServer.Hubs;

/// <summary>
/// This file handles the update permissions for the client caller, or a client callers paired user.
/// <para>
/// Which is handled is determined by the function call name, and verification checks will be made.
/// </para>
/// </summary>
public partial class GagspeakHub : Hub<IGagspeakHub>, IGagspeakHub
{
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
        if (globalPerms == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Pair permission not found").ConfigureAwait(false);
            return;
        }

        // establish the keyvalue pair from the Dto so we know what is changing.
        string propertyName = dto.ChangedPermission.Key;
        object newValue = dto.ChangedPermission.Value;
        // Get the PropertyInfo object for the property with the matching name in globalPerms
        PropertyInfo propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // [YES THIS IS WHERE IT UPDATES THE ACTUAL GLOBALPERMS OBJECT]
                propertyInfo.SetValue(globalPerms, newValue);
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
        PropertyInfo propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // [YES THIS IS WHERE IT UPDATES THE ACTUAL GLOBALPERMS OBJECT]
                propertyInfo.SetValue(globalPerms, newValue);
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

        // establish the keyvalue pair from the Dto so we know what is changing.
        string propertyName = dto.ChangedPermission.Key;
        object newValue = dto.ChangedPermission.Value;
        PropertyInfo propertyInfo = typeof(UserPairPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // [YES THIS IS WHERE IT UPDATES THE ACTUAL PAIRPERMS OBJECT]
                propertyInfo.SetValue(pairPerms, newValue);
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

        // send a callback to the userpair we updated our permission for, so they get the updated info
        await Clients.User(dto.User.UID).Client_UserUpdateOtherPairPerms(dto).ConfigureAwait(false);
        // callback the updated info to the client caller as well so it can update properly.
        await Clients.Caller.Client_UserUpdateSelfPairPerms(dto).ConfigureAwait(false);
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
        PropertyInfo propertyInfo = typeof(UserPairPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // [YES THIS IS WHERE IT UPDATES THE ACTUAL PAIRPERMS OBJECT]
                propertyInfo.SetValue(pairPerms, newValue);
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
        await Clients.User(dto.User.UID).Client_UserUpdateSelfPairPerms(dto).ConfigureAwait(false);
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
        PropertyInfo propertyInfo = typeof(UserEditAccessPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // Set the new value for the property
                propertyInfo.SetValue(pairAccess, newValue);
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

        // send a callback to the userpair we updated our permission for, so they get the updated info
        await Clients.User(dto.User.UID).Client_UserUpdateOtherPairPermAccess(dto).ConfigureAwait(false);
        // callback the updated info to the client caller as well so it can update properly.
        await Clients.Caller.Client_UserUpdateSelfPairPermAccess(dto).ConfigureAwait(false);
    }
}
