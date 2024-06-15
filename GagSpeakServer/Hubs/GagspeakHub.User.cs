using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gagspeak.API.Data;
using Gagspeak.API.Data.Enum;
using Gagspeak.API.Data.CharacterData;
using Gagspeak.API.Dto;
using Gagspeak.API.Dto.User;
using GagspeakServer.Utils;
using GagSpeakServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using GagspeakServer.Models;

namespace GagspeakServer.Hubs;

/// <summary> This partial class of the GagspeakHub contains all the user related methods </summary>
public partial class GagspeakHub
{

    /// <summary> Called by a connected client who wishes to add another User as a pair.
    /// <para> The Dto that the user sends with this request contains the UserData of the person they wish to add.</para>
    /// </summary>
    /// <param name="dto">The User Dto of the player they desire to add.</param>
    [Authorize(Policy = "Identified")]
    public async Task UserAddPair(UserDto dto)
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // temporarily store the UID of the user the client caller is trying to add from the dto passed in.
        var uid = dto.User.UID.Trim();

        // check to see if the user is trying to add themselves as a pair or if the UID is empty. If it is, just flat out return.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(dto.User.UID)) return;

        // fetch the other user that the client caller is trying to add as a pair from the database by searching for the UID or Alias.
        var otherUser = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == uid || u.Alias == uid).ConfigureAwait(false);
        // if the resulting variable is null, then we were not able to find them because they do not yet exist.
        if (otherUser == null)
        {
            // send a message to the client caller that the user they are trying to add does not exist, and then perform an early return
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, UID does not exist").ConfigureAwait(false);
            return;
        }

        // if the client caller is trying to add themselves...
        if (string.Equals(otherUser.UID, UserUID, StringComparison.Ordinal))
        {
            // send a message to the client caller that they cannot pair with themselves, and then perform an early return
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Im not quite sure why you are trying to pair to yourself, but please dont.").ConfigureAwait(false);
            return;
        }

        // check to see if the client caller is already paired with the user they are trying to add.
        var existingEntry =
            await DbContext.ClientPairs.AsNoTracking() // search the client pairs table in the database
                .FirstOrDefaultAsync(p =>               // for the first or default entry where the user UID matches the client caller's UID
                    p.User.UID == UserUID && p.OtherUserUID == otherUser.UID).ConfigureAwait(false);

        // if that entry does exist,
        if (existingEntry != null)
        {
            // send a message back to the client caller to inform them they are already paired with this user, and then perform an early return
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, $"Cannot pair with {dto.User.UID}, already paired").ConfigureAwait(false);
            return;
        }

        // grab outselves from the database (our UID is stored in the Hub.Functions.cs as UserUID)
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // log that the caller has successfully added the user as a pair
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));

        // create a new client pair object, wherein the variables are set to the details of the other user and the client caller
        ClientPair wl = new ClientPair()
        {
            OtherUser = otherUser,
            User = user,
        };
        // append it to the database
        await DbContext.ClientPairs.AddAsync(wl).ConfigureAwait(false);

        // fetch the existing data of the pair from the database using a helper function in the Hub.Functions.cs
        var existingData = await GetPairInfo(UserUID, otherUser.UID).ConfigureAwait(false);

        //  The following below has to do with permission handling, perhaps we can repurpose this later. but for now, dont worry about it.
        /*var permissions = existingData?.OwnPermissions;
        if (permissions == null || !permissions.Sticky)
        {
            var ownDefaultPermissions = await DbContext.UserDefaultPreferredPermissions.AsNoTracking().SingleOrDefaultAsync(f => f.UserUID == UserUID).ConfigureAwait(false);

            permissions = new UserPermissionSet()
            {
                User = user,
                OtherUser = otherUser,
                DisableAnimations = ownDefaultPermissions.DisableIndividualAnimations,
                DisableSounds = ownDefaultPermissions.DisableIndividualSounds,
                DisableVFX = ownDefaultPermissions.DisableIndividualVFX,
                IsPaused = false,
                Sticky = true
            };

            var existingDbPerms = await DbContext.Permissions.SingleOrDefaultAsync(u => u.UserUID == UserUID && u.OtherUserUID == otherUser.UID).ConfigureAwait(false);
            if (existingDbPerms == null)
            {
                await DbContext.Permissions.AddAsync(permissions).ConfigureAwait(false);
            }
            else
            {
                existingDbPerms.DisableAnimations = permissions.DisableAnimations;
                existingDbPerms.DisableSounds = permissions.DisableSounds;
                existingDbPerms.DisableVFX = permissions.DisableVFX;
                existingDbPerms.IsPaused = false;
                existingDbPerms.Sticky = true;

                DbContext.Permissions.Update(existingDbPerms);
            }
        }*/

        // update the database with the new pair
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // we need to now obtain the other user to validate permissions and pair direction status, so fetch the opposite entry with a helper.
        ClientPair otherEntry = OppositeEntry(otherUser.UID);

        // the identity of the other user, fetched from the database
        var otherIdent = await GetUserIdent(otherUser.UID).ConfigureAwait(false);

        // construct a new UserPairDto based on the responce
        UserPairDto userPairResponse = new UserPairDto(otherUser.ToUserData(),
            otherEntry == null ? IndividualPairStatus.OneSided : IndividualPairStatus.Bidirectional);

        // send back to the client caller the invokable function Client_UserAddClientPair,
        // forcing that client to add the userPairResponce to their pair manager (or handler? i forget its everywhere lol)
        await Clients.User(user.UID).Client_UserAddClientPair(userPairResponse).ConfigureAwait(false);

        // if the other entry / other ident is null, then we dont need to do anything further
        if (otherIdent == null || otherEntry == null) return;

        // but if they are, we should push to the other connected user that they have been added as a pair.
        await Clients.User(otherUser.UID)
            .Client_UpdateUserIndividualPairStatusDto(new(user.ToUserData(), IndividualPairStatus.Bidirectional))
            .ConfigureAwait(false);

        // send to both the client caller and the other user that the paired people are online
        await Clients.User(UserUID).Client_UserSendOnline(new(otherUser.ToUserData(), otherIdent)).ConfigureAwait(false);
        await Clients.User(otherUser.UID).Client_UserSendOnline(new(user.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
    }

    /// <summary> Called by a connected client who wishes to delete their account from the server.
    /// <para>
    /// This requires the client caller to be authroized under the policy 
    /// "Identified", meaning they have passed authorization.
    /// </para>
    /// <para> 
    /// Method will remove all associated things with the user and delete their profile from 
    /// the server, along with all other profiles under their account.
    /// </para>
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserDelete()
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo();

        // fetch the client callers user data from the database.
        var userEntry = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // for any other profiles registered under this account, fetch them from the database as well.
        var secondaryUsers = await DbContext.Auth.Include(u => u.User).Where(u => u.PrimaryUserUID == UserUID).Select(c => c.User).ToListAsync().ConfigureAwait(false);
        // remove all the client callers secondary profiles, then finally, remove their primary profile. (dont through helper functions)
        foreach (var user in secondaryUsers)
        {
            await DeleteUser(user).ConfigureAwait(false);
        }
        await DeleteUser(userEntry).ConfigureAwait(false);
    }

    /// <summary> Called by a connected client wishing to retrieve the pair objects
    /// from the users they are paired with who are online.
    /// <para> This requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>
    /// <returns>The OnlineUserIdentDto list of all the client-callers paired users who are currently connected</returns>
    [Authorize(Policy = "Identified")]
    public async Task<List<OnlineUserIdentDto>> UserGetOnlinePairs()
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo();

        // fetch all users who are paired with the requesting client caller. (online and offline)
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // obtain a list of all the paired users who are currently online.
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // send that you are online to all connected online pairs of the client caller first..
        await SendOnlineToAllPairedUsers().ConfigureAwait(false);

        // then, return back to the client caller the list of all the online users they are paired with.
        return pairs.Select(p => new OnlineUserIdentDto(new UserData(p.Key), p.Value)).ToList();
    }


    /// <summary> Called by a connected client who wishes to retrieve the list of paired clients via a list of UserPairDto's.
    /// <para> Requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>
    /// <returns> A list of UserPairDto's containing the paired clients of the client caller</returns>
    [Authorize(Policy = "Identified")]
    public async Task<List<UserPairDto>> UserGetPairedClients()
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo();

        // fetch all the pair information of the client caller
        var pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        // return the list of UserPairDto's containing the paired clients of the client caller
        return pairs.Select(p => {
            return new UserPairDto(new UserData(p.Key, p.Value.Alias), p.Value.ToIndividualPairStatus());
        }).ToList();
    }

    /// <summary> Called by a connected client who wishes to retrieve the profile of another user.
    /// <para> Requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>
    /// <returns> The UserProfileDto of the user requested</returns>
    [Authorize(Policy = "Identified")]
    public async Task<UserProfileDto> UserGetProfile(UserDto user)
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo(GagspeakHubLogger.Args(user));

        // fetch all the paired users of the client caller
        var allUserPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        // all userpairs do not contain the user UID of the user requested, and the user UID is not the client caller.
        if (!allUserPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            // return a dummy userProfileDto to let the client caller know they cannot access the profile of the user requested. while idling their connection.
            return new UserProfileDto(user.User, false, null, "Due to the pause status you cannot access this users profile.");
        }

        // otherwise we are valid to fetch the profile of the user requested, so attmept to locate the profile in the database.
        UserProfileData? data = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.User.UID).ConfigureAwait(false);

        // if the profile of the user requested is null, return a empty profile to the client caller.
        if (data == null) return new UserProfileDto(user.User, false, null, null);

        // if the profile is disabled, return a disabled profile to the client caller.
        if (data.ProfileDisabled) return new UserProfileDto(user.User, true, null, "This profile is currently disabled");

        // otherwise, it is a valid profile, so return it.
        return new UserProfileDto(user.User, false, data.Base64ProfilePic, data.UserDescription);
    }


    /// <summary> Called by a connected client who wishes to push an update of their character data to the server. (their permissions / GagSpeak state has changed)
    /// <para> Requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>
    /// <param name="dto">the UserCharaDataMessage Dto, containing the character data of the client caller, and the list of recipient pairs they should be sent to.</param>
    [Authorize(Policy = "Identified")]
    public async Task UserPushData(UserCharaDataMessageDto dto)
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo();

        // initially assume that the pushed data is valid, so set the invalid variable to false;
        bool hadInvalidData = false;

        // thankfully, unlike gagspeak, we dont need to worry about file validation, because everything is transferred over the Dto's and variables.

        // fetch the recipient UID list from the recipient list of the dto
        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        // check to see if all the recipients are cached, if not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);

        // if not all players are cached.
        if (!allCached)
        {
            // fetch all the paired users of the client caller
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            // see which of the paired users are in the recipient list
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            // cache those players
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }
        // log how many recipients are in the recipient list that we are pushing our data to.
        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));

        // send back to all the connected recipient UIDs a invoked function UserReceiveCharacterData, containing the character data of the client caller to update.
        await Clients.Users(recipientUids).Client_UserReceiveCharacterData(new OnlineUserCharaDataDto(new UserData(UserUID), dto.CharaData)).ConfigureAwait(false);

        // log that the counter for pushing data was inc (if we ever have metrics at any point) (just look in gagspeaks code later to see how to integrate if lost.
    }

    /// <summary> Called by a connected client who wishes to remove a user from their paired list.
    /// <para> Requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>>
    [Authorize(Policy = "Identified")]
    public async Task UserRemovePair(UserDto dto)
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // see if the client callers UserUID is equal to the UID in the passed in dto to remove. if so, dont let them remove themselves and return
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) return;

        // otherwise, check to see if the pair even exists in the database for the client callers pairs at all.
        ClientPair? callerPair =
            await DbContext.ClientPairs.SingleOrDefaultAsync(w => w.UserUID == UserUID && w.OtherUserUID == dto.User.UID).ConfigureAwait(false);

        // if it doesnt, then return.
        if (callerPair == null) return;

        // if it does, then get the pair info of the client caller and the user they are trying to remove.
        var pairData = await GetPairInfo(UserUID, dto.User.UID).ConfigureAwait(false);

        // delete the pair from the database
        DbContext.ClientPairs.Remove(callerPair);
        // update the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // log that the pair removeal was successful
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto, "Success"));

        // return to the client caller the invokable function Client_UserRemoveClientPair, which lets them know the pair was removed.
        await Clients.User(UserUID).Client_UserRemoveClientPair(dto).ConfigureAwait(false);

        // see if the pair data was not individually paired, if it wasnt, then return
        if (!pairData.IndividuallyPaired) return;

        // otherwise, if it was, then grab the other users identity from the database. if it is null, return.
        var otherIdent = await GetUserIdent(dto.User.UID).ConfigureAwait(false);
        if (otherIdent == null) return;

        // otherwise, if it did exsit, send an invokable function to the other user's client to update their
        // pair status with the person who just removed them, replacing it from bidirectional to one sided.
        await Clients.User(dto.User.UID)
            .Client_UpdateUserIndividualPairStatusDto(new(new(UserUID), IndividualPairStatus.OneSided))
            .ConfigureAwait(false);
        // there was a conditional here i didnt think was nessisary, but if we get errors here, then we will know why
.       await Clients.User(UserUID).Client_UserSendOffline(dto).ConfigureAwait(false);
        await Clients.User(dto.User.UID).Client_UserSendOffline(new(new(UserUID))).ConfigureAwait(false);
    }

    /// <summary> Called by a connected client who wishes to set or update their profile data.
    /// <para> Requires the client caller to be authroized under the policy "Identified", meaning they have passed authorization.</para>
    /// </summary>
    /// <param name="dto">the userProfile Dto, containing both the content of the profile, the and UID of the person it belongs to. </param>
    [Authorize(Policy = "Identified")]
    public async Task UserSetProfile(UserProfileDto dto)
    {
        // log the call information so we know a user has requested it.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // check to see if the user UID of the profile dto is the same as the client caller, if not, return.
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new HubException("Cannot modify profile data for anyone but yourself");

        // fetch the existing profile data of the client caller from the database.
        var existingData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);

        // if the profile is disabled, return a error message to the client caller and return. (remove later maybe? idk)
        if (existingData?.ProfileDisabled ?? false)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your profile was permanently disabled and cannot be edited").ConfigureAwait(false);
            return;
        }

        // if the profile picture base64 string is not empty, then we need to validate the image.
        if (!string.IsNullOrEmpty(dto.ProfilePictureBase64))
        {
            // convert the base64 string to a byte array
            byte[] imageData = Convert.FromBase64String(dto.ProfilePictureBase64);
            // load the image into a memory stream
            using MemoryStream ms = new(imageData);
            // detect the format the image
            var format = await Image.DetectFormatAsync(ms).ConfigureAwait(false);
            // if the file format is not a png, reject the image and return.
            if (!format.FileExtensions.Contains("png", StringComparer.OrdinalIgnoreCase))
            {
                // invoke a function call to the client caller containing a server message letting them know that they provided a non-png image.
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is not in PNG format").ConfigureAwait(false);
                return;
            }
            
            // the image is a png, load the image into a memory stream
            using var image = Image.Load<Rgba32>(imageData);

            // if the image is larger than 256x256 or more than 250KiB, reject the image and return.
            if (image.Width > 256 || image.Height > 256 || (imageData.Length > 250 * 1024))
            {
                // invoke a function call to the client caller containing a server message letting them know that they provided a large image.
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Error, "Your provided image file is larger than 256x256 or more than 250KiB.").ConfigureAwait(false);
                return;
            }
        }

        // if the existing data is not null, then we need to update the existing data with the new data.
        if (existingData != null)
        {
            // if the incoming profilepicture is empty, set it to null.
            if (string.Equals("", dto.ProfilePictureBase64, StringComparison.OrdinalIgnoreCase))
            {
                existingData.Base64ProfilePic = null;
            }
            // see if the new profile image is not null, if it is not, then update it
            else if (dto.ProfilePictureBase64 != null)
            {
                existingData.Base64ProfilePic = dto.ProfilePictureBase64;
            }
            // finally, if the description is not null, update it.
            if (dto.Description != null)
            {
                existingData.UserDescription = dto.Description;
            }
        }
        else // hitting this else means that the existing data was null, so we need to construct a new UserProfileData object for it.
        {
            // create a new UserProfileData object with the user UID of the client caller
            UserProfileData userProfileData = new()
            {
                UserUID = dto.User.UID,
                Base64ProfilePic = dto.ProfilePictureBase64 ?? null,
                UserDescription = dto.Description ?? null,
            };

            // add the userprofiledata to the database.
            await DbContext.UserProfileData.AddAsync(userProfileData).ConfigureAwait(false);
        }

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch all paired users of the client caller
        var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // get all the online pairs of the client callers paired list 
        var pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);

        // invokes the Client_UserUpdateProfile method on all connected clients whose user IDs are specified
        // in the pairs collection, and sends the updated user profile information (dto.User) to the clients.
        await Clients.Users(pairs.Select(p => p.Key)).Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
        // invoke the client_userUpdateProfile method at the client caller that made the request.
        await Clients.Caller.Client_UserUpdateProfile(new(dto.User)).ConfigureAwait(false);
    }

    /// <summary> A small helper function to get the opposite entry of a client pair (how its viewed from the other side) </summary>
    private ClientPair OppositeEntry(string otherUID) => DbContext.ClientPairs.AsNoTracking().SingleOrDefault(w => w.User.UID == otherUID && w.OtherUser.UID == UserUID);
}