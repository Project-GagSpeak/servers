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
/// 
/// This file is the Achilles' heel of my brain damage while writing this. I had no idea what other way to
/// modularize this in a way that wasnt just copy paste spam since everything is just unique enough to the point 
/// where you cant really link everything together since the DTO is based off the API model which is based off the DB
/// model, and you cant really just reference the DB model directly since it needs to go to the client which needs
/// to use the API for reference
/// 
/// <para>
/// 
/// I am begging you, if you find a better way to structure this relationship between the DTO, API, and DB models,
/// please contact me and show me how. I fucking hate this current approach with passion.
/// 
/// </para>
/// 
/// </summary>
public partial class GagspeakHub : Hub<IGagspeakHub>, IGagspeakHub
{
    /// <summary>
    /// 
    /// Updates the default global permissions of the client who has called it. (For now, this only works for the user who called it.)
    /// 
    /// </summary>
    /// <param name="defaultPermissions"></param>
    /// <returns></returns>
    [Authorize(Policy = "Authenticated")]
    public async Task UserUpdateGlobalPerms(UserGlobalPermChangeDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var globalPerms = await DbContext.UserGlobalPermissions.SingleAsync(u => u.UserUID == UserUID).ConfigureAwait(false);

        string propertyName = dto.ChangedPermission.Key;
        object newValue = dto.ChangedPermission.Value;


        // Get the PropertyInfo object for the property with the matching name in globalPerms
        PropertyInfo propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);

        if (propertyInfo != null)
        {
            // Check if the property is one of the non-modifiable properties
            if (!(dto.User.UID == UserUID) && (propertyName == nameof(UserGlobalPermissions.Safeword) || propertyName == nameof(UserGlobalPermissions.SafewordUsed)))
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Don't try modifying something you shouldn't").ConfigureAwait(false);
                return;
            }
            else
            {
                // Ensure the type of the newValue matches the property type
                if (propertyInfo.PropertyType == newValue.GetType())
                {
                    // Set the new value for the property
                    propertyInfo.SetValue(globalPerms, newValue);
                }
                else
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type missmatch on requested update").ConfigureAwait(false);
                    return;
                }
            }
        }
        else
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Property to modify not found").ConfigureAwait(false);
            return;
        }

        DbContext.Update(globalPerms);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // grab the user pairs of the client caller
        List<string> allPairedUsersOfClient = new();
        if (dto.User.UID == UserUID)
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        }
        else
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        }
        var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

        // callback to our pairs to let them know that our permissions have been updated.
        await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherPairPermsGlobal(dto).ConfigureAwait(false);
        // callback to the client caller to let them know that their permissions have been updated.
        await Clients.Users(dto.User.UID).Client_UserUpdateSelfPairPermsGlobal(dto).ConfigureAwait(false);
    }


    [Authorize(Policy = "Authenticated")]
    public async Task UserUpdatePairPerms(UserPairPermChangeDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var pairPerms = await DbContext.ClientPairPermissions.SingleAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);

        string propertyName = dto.ChangedPermission.Key;
        object newValue = dto.ChangedPermission.Value;

        // Get the PropertyInfo object for the property with the matching name in pairPerms
        PropertyInfo propertyInfo = typeof(UserPairPermissions).GetProperty(propertyName);
        if (propertyInfo != null)
        {
            // Ensure the type of the newValue matches the property type
            if (propertyInfo.PropertyType == newValue.GetType())
            {
                // Set the new value for the property
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

        // grab the user pairs of the client caller
        List<string> allPairedUsersOfClient = new();
        if (dto.User.UID == UserUID)
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        }
        else
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        }
        var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

        // callback to our pairs to let them know that our permissions have been updated.
        await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherPairPerms(dto).ConfigureAwait(false);
        // callback to the client caller to let them know that their permissions have been updated.
        await Clients.Users(dto.User.UID).Client_UserUpdateSelfPairPerms(dto).ConfigureAwait(false);
    }

    [Authorize(Policy = "Authenticated")]
    public async Task UserUpdatePairPermAccess(UserPairAccessChangeDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var pairAccess = await DbContext.ClientPairPermissionAccess.SingleAsync(u => u.UserUID == UserUID && u.OtherUserUID == dto.User.UID).ConfigureAwait(false);

        string propertyName = dto.ChangedAccessPermission.Key;
        object newValue = dto.ChangedAccessPermission.Value;

        // Get the PropertyInfo object for the property with the matching name in pairAccess
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
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Type missmatch on requested update").ConfigureAwait(false);
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

        // grab the user pairs of the client caller
        List<string> allPairedUsersOfClient = new();
        if (dto.User.UID == UserUID)
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        }
        else
        {
            allPairedUsersOfClient = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        }
        var pairsOfClient = await GetOnlineUsers(allPairedUsersOfClient).ConfigureAwait(false);

        // callback to our pairs to let them know that our permissions have been updated.
        await Clients.Users(pairsOfClient.Select(p => p.Key)).Client_UserUpdateOtherPairPermAccess(dto).ConfigureAwait(false);
        // callback to the client caller to let them know that their permissions have been updated.
        await Clients.Users(dto.User.UID).Client_UserUpdateSelfPairPermAccess(dto).ConfigureAwait(false);
    }
}
