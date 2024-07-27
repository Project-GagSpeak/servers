using GagspeakAPI.Data.Enum;
using GagspeakAPI.Data.VibeServer;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Toybox;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary>
/// Partial class dealing with the main toybox hub room / group functionality.
/// </summary>
public partial class ToyboxHub
{
    /// <summary> Create a new room. </summary>
    public async Task<bool> PrivateRoomCreate(RoomCreateDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));

        // if the user is already a host of any room, we must inform them they have to close it first.
        var existingRoomsMadeByHost = await DbContext.PrivateRooms.AnyAsync(r => r.HostUID == UserUID).ConfigureAwait(false);
        if (existingRoomsMadeByHost)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Warning,
                $"Already a Host of another Room. Close it before creating a new one!").ConfigureAwait(false);
            return false;
        }

        // see if we are InRoom for any other room.
        var isActivelyInRoom = await DbContext.PrivateRoomPairs
            .AnyAsync(pru => pru.PrivateRoomUserUID == UserUID && pru.InRoom).ConfigureAwait(false);

        if (isActivelyInRoom)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Warning,
                $"Already in a Room. Leave it before creating a new one!").ConfigureAwait(false);
            return false;
        }

        // Check if the room name already exists in the database
        var roomExists = DbContext.PrivateRooms.Any(r => r.NameID == dto.NewRoomName);
        if (roomExists)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Warning, "Room already in use.").ConfigureAwait(false);
            return false;
        }

        // At this point we are valid and can create a new room. So begin.
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);

        // Create new Private Room, set creation time, & add to database
        var newRoom = new PrivateRoom
        {
            NameID = dto.NewRoomName,
            HostUID = UserUID,
            Host = user,
            TimeMade = DateTime.UtcNow
        };
        DbContext.PrivateRooms.Add(newRoom);

        // Make new PrivateRoomPair, with the Client Caller as the first user in the room.
        var newRoomUser = new PrivateRoomPair
        {
            PrivateRoomNameID = dto.NewRoomName,
            PrivateRoom = newRoom,
            PrivateRoomUserUID = UserUID,
            PrivateRoomUser = user,
            ChatAlias = dto.HostChatAlias,
            InRoom = false,
            AllowingVibe = false
        };
        DbContext.PrivateRoomPairs.Add(newRoomUser);

        // Save changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // add the user as a host of the room in the concurrent dictionary
        RoomHosts.TryAdd(dto.NewRoomName, UserUID);

        // Notify client caller in notifications that room has been created.
        await Clients.Caller.Client_ReceiveToyboxServerMessage
            (MessageSeverity.Information, $"Room {dto.NewRoomName} created.").ConfigureAwait(false);

        // Collect and map the list of PrivateRoomPairs in the room to PrivateRoomUsers
        var privateRoomUsers = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == dto.NewRoomName)
            .Select(pru => new PrivateRoomUser
            { 
                UserUID = pru.PrivateRoomUserUID, 
                ChatAlias = pru.ChatAlias, 
                ActiveInRoom = pru.InRoom, 
                VibeAccess = pru.AllowingVibe 
            })
            .ToListAsync()
            .ConfigureAwait(false);

        // Compile RoomInfoDto for the client caller.
        var roomInfo = new RoomInfoDto
        {
            NewRoomName = dto.NewRoomName,
            RoomHost = new PrivateRoomUser
            { 
                UserUID = UserUID, 
                ChatAlias = newRoomUser.ChatAlias, 
                ActiveInRoom = newRoomUser.InRoom, 
                VibeAccess = newRoomUser.AllowingVibe 
            },
            ConnectedUsers = privateRoomUsers
        };
        await Clients.Caller.Client_PrivateRoomJoined(roomInfo).ConfigureAwait(false);
        return true;
    }

    /// <summary> Recieve a room invite from an added userpair. </summary>
    public async Task<bool> PrivateRoomInviteUser(RoomInviteDto dto)
    {
        // ensure the client caller inviting the user is the host of the room they are inviting.
        var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == dto.RoomName).ConfigureAwait(false);
        // if not valid, return.
        if (room == null || !string.Equals(room.HostUID, UserUID, StringComparison.Ordinal)) return false;

        // grab the caller from the db
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user == null) return false;

        // send the invite to the user.
        await Clients.User(dto.UserInvited.UID).Client_UserReceiveRoomInvite
            (new RoomInviteDto(user.ToUserData(), dto.RoomName)).ConfigureAwait(false);
        // return successful.
        return true;
    }

    /// <summary> A User attempting to join a room. </summary>
    public async Task PrivateRoomJoin(RoomParticipantDto userJoining)
    {
        // check to see if the client caller is currently active in any other rooms.
        var isActivelyInRoom = await DbContext.PrivateRoomPairs
            .AnyAsync(pru => pru.PrivateRoomUserUID == UserUID && pru.InRoom).ConfigureAwait(false);

        if (isActivelyInRoom)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage(MessageSeverity.Warning,
                $"Already in a Room. Leave it before creating a new one!").ConfigureAwait(false);
            throw new Exception($"You are already in a Private Room. You must leave the current Room to join a new one!");
        }

        // Ensure the room exists.
        var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == userJoining.RoomName).ConfigureAwait(false);
        if (room == null)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage
                (MessageSeverity.Error, $"Room {userJoining.RoomName} does not exist, aborting join.").ConfigureAwait(false);
            throw new Exception($"Room {userJoining.RoomName} does not exist, aborting join.");
        }

        // grab the caller from the db
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user == null) return;


        // if the user joining already has a pair for the privateroompairs of this room, simply set InRoom to true
        var existingRoomUser = await DbContext.PrivateRoomPairs
            .FirstOrDefaultAsync(pru => pru.PrivateRoomNameID == userJoining.RoomName && pru.PrivateRoomUserUID == UserUID)
            .ConfigureAwait(false);

        PrivateRoomPair currentRoomUser;
        if (existingRoomUser != null)
        {
            existingRoomUser.InRoom = true;
            DbContext.PrivateRoomPairs.Update(existingRoomUser);
            currentRoomUser = existingRoomUser;
        }
        else
        {
            // they did not exist, so make a new pair for them.
            var newRoomUser = new PrivateRoomPair
            {
                PrivateRoomNameID = userJoining.RoomName,
                PrivateRoom = room,
                PrivateRoomUserUID = user.UID,
                PrivateRoomUser = user,
                ChatAlias = userJoining.User.ChatAlias,
                InRoom = true,
                AllowingVibe = false
            };
            DbContext.PrivateRoomPairs.Add(newRoomUser);
            currentRoomUser = newRoomUser;
        }
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // fetch the list of all others users currently in the room (active or not)
        var RoomParticipants = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == userJoining.RoomName)
            .ToListAsync()
            .ConfigureAwait(false);

        // fetch the list of all active users currently in the same  room we just fetched above
        var ActiveParticipants = RoomParticipants
            .Where(pru => pru.InRoom)
            .Select(pru => new PrivateRoomUser
            {
                UserUID = pru.PrivateRoomUserUID,
                ChatAlias = pru.ChatAlias,
                ActiveInRoom = pru.InRoom,
                VibeAccess = pru.AllowingVibe
            })
            .ToList();

        // find the host of the room that is in the private room pairs.
        var roomHost = RoomParticipants.FirstOrDefault(pru => string.Equals(pru.PrivateRoomUserUID, room.HostUID, StringComparison.Ordinal));
        if (roomHost == null) return;


        // Send a notification to the active users in the room that a new user has joined
        await Clients.Users(ActiveParticipants.Select(u => u.UserUID).ToList()).Client_ReceiveToyboxServerMessage
            (MessageSeverity.Information, $"{currentRoomUser.ChatAlias} has joined the room.").ConfigureAwait(false);

        // send a OtherUserJoinedRoom update to all room participants, so their room information is updated.
        var newJoinedUser = new PrivateRoomUser
        {
            UserUID = currentRoomUser.PrivateRoomUserUID,
            ChatAlias = currentRoomUser.ChatAlias,
            ActiveInRoom = currentRoomUser.InRoom,
            VibeAccess = currentRoomUser.AllowingVibe
        };
        // extact our client caller (current room user) from the list of room participants.
        RoomParticipants.Remove(currentRoomUser);
        // here.
        await Clients.Users(RoomParticipants.Select(u => u.PrivateRoomUserUID).ToList())
            .Client_PrivateRoomOtherUserJoined(new RoomParticipantDto(newJoinedUser, userJoining.RoomName)).ConfigureAwait(false);

        // compile RoomInfoDto to send to the client caller.
        var roomInfo = new RoomInfoDto
        {
            NewRoomName = userJoining.RoomName,
            RoomHost = new PrivateRoomUser
            {
                UserUID = roomHost.PrivateRoomUserUID,
                ChatAlias = roomHost.ChatAlias,
                ActiveInRoom = roomHost.InRoom,
                VibeAccess = roomHost.AllowingVibe
            },
            ConnectedUsers = RoomParticipants.Select(pru => new PrivateRoomUser
            {
                UserUID = pru.PrivateRoomUserUID,
                ChatAlias = pru.ChatAlias,
                ActiveInRoom = pru.InRoom,
                VibeAccess = pru.AllowingVibe
            }).ToList()
        };
        // send the room info to the client caller.
        await Clients.Caller.Client_PrivateRoomJoined(roomInfo).ConfigureAwait(false);
    }

    /// <summary> Send a message to the users in the room you are in. </summary>
    public async Task PrivateRoomSendMessage(RoomMessageDto dto)
    {
        // Ensure the user is in the room they are trying to send a message to, and that they are active in it.
        var isActivelyInRoom = await IsActiveInRoom(dto.RoomName).ConfigureAwait(false);

        if (!isActivelyInRoom) throw new Exception("Failed to locate your user as an active participant in the room you sent the update to.");

        // grab the list of active participants in the room.
        var activeRoomParticipantsUID = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == dto.RoomName && pru.InRoom)
            .Select(u => u.PrivateRoomUserUID)
            .ToListAsync()
            .ConfigureAwait(false);

        // Send the message to active participants in the private room
        await Clients.Users(activeRoomParticipantsUID).Client_PrivateRoomMessage(dto).ConfigureAwait(false);
    }

    /// <summary> Push new device info to the participants in the room. </summary>
    public async Task PrivateRoomPushDevice(UserCharaDeviceInfoMessageDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));

        // ensure we are actively in the room we are trying to send the device info to.
        var isActivelyInRoom = await IsActiveInRoom(dto.RoomName).ConfigureAwait(false);

        if (!isActivelyInRoom) throw new Exception("Failed to locate your user as an active participant in the room you sent the update to.");

        // if valid, send the updated device info to the users active in the room
        // grab the list of active participants in the room.
        var activeRoomParticipantsUID = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == dto.RoomName && pru.InRoom)
            .Select(u => u.PrivateRoomUserUID)
            .ToListAsync()
            .ConfigureAwait(false);

        // Send the message to active participants in the private room
        await Clients.Users(activeRoomParticipantsUID).Client_PrivateRoomReceiveUserDevice(dto).ConfigureAwait(false);
    }

    /// <summary> Adds client caller to the rooms group. </summary>
    public async Task PrivateRoomAllowVibes(string roomName)
    {
        _logger.LogCallInfo();
        // verify the user is active in a room.
        var isActivelyInRoom = await IsActiveInRoom(roomName).ConfigureAwait(false);

        if (!isActivelyInRoom) throw new Exception("Failed to locate your user as an active participant in the room you sent the update to.");

        // add the user to the group for the room.
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName).ConfigureAwait(false);
        // add the user to the RoomContextGroupVibeUsers for the RoomName 
        RoomContextGroupVibeUsers[roomName].Add(UserUID);

        // grab the privateroompair that just allowed this
        var roomPair = await DbContext.PrivateRoomPairs
            .FirstOrDefaultAsync(pru => pru.PrivateRoomNameID == roomName && pru.PrivateRoomUserUID == UserUID)
            .ConfigureAwait(false);
        if (roomPair == null) return;

        // update the pair to allow vibes & save the changes.
        roomPair.AllowingVibe = true;

        DbContext.PrivateRoomPairs.Update(roomPair);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // export the update.
        var roomuser = new PrivateRoomUser
        {
            UserUID = roomPair.PrivateRoomUserUID,
            ChatAlias = roomPair.ChatAlias,
            ActiveInRoom = roomPair.InRoom,
            VibeAccess = roomPair.AllowingVibe
        };

        // send the update to the user that they have been added to the group.
        var activeRoomParticipantsUID = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == roomName && pru.InRoom)
            .Select(u => u.PrivateRoomUserUID)
            .ToListAsync()
            .ConfigureAwait(false);

        // Send the message to active participants in the private room
        await Clients.Users(activeRoomParticipantsUID).Client_PrivateRoomUpdateUser
            (new RoomParticipantDto(roomuser, roomName)).ConfigureAwait(false);
    }

    /// <summary> Adds client caller to the rooms group. </summary>
    public async Task PrivateRoomDenyVibes(string roomName)
    {
        _logger.LogCallInfo();
        // verify the user is active in a room.
        var isActivelyInRoom = await IsActiveInRoom(roomName).ConfigureAwait(false);

        if (!isActivelyInRoom) throw new Exception("Failed to locate your user as an active participant in the room you sent the update to.");

        // add the user to the group for the room.
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName).ConfigureAwait(false);
        // add the user to the RoomContextGroupVibeUsers for the RoomName 
        RoomContextGroupVibeUsers[roomName].Remove(UserUID);

        // grab the privateroompair that just allowed this
        var roomPair = await DbContext.PrivateRoomPairs
            .FirstOrDefaultAsync(pru => pru.PrivateRoomNameID == roomName && pru.PrivateRoomUserUID == UserUID)
            .ConfigureAwait(false);
        if (roomPair == null) return;

        // update the pair to allow vibes & save the changes.
        roomPair.AllowingVibe = false;

        DbContext.PrivateRoomPairs.Update(roomPair);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // export the update.
        var roomuser = new PrivateRoomUser
        {
            UserUID = roomPair.PrivateRoomUserUID,
            ChatAlias = roomPair.ChatAlias,
            ActiveInRoom = roomPair.InRoom,
            VibeAccess = roomPair.AllowingVibe
        };

        // send the update to the user that they have been added to the group.
        var activeRoomParticipantsUID = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == roomName && pru.InRoom)
            .Select(u => u.PrivateRoomUserUID)
            .ToListAsync()
            .ConfigureAwait(false);

        // Send the message to active participants in the private room
        await Clients.Users(activeRoomParticipantsUID).Client_PrivateRoomUpdateUser
            (new RoomParticipantDto(roomuser, roomName)).ConfigureAwait(false);
    }


    /// <summary> 
    /// This call spesifically is called in a streamlined mannor, meaning processing time for it
    /// should be very minimal.
    /// 
    /// To account for this, interactions for this only occur for connected users in a hub context group.
    /// Updates can only be called by the host of the room. 
    /// </summary>
    public async Task PrivateRoomUpdateUserDevice(UpdateDeviceDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));
        // make sure we are the host of the room we are sending the update to.
        if (!RoomHosts.TryGetValue(dto.RoomName, out var roomHost) || roomHost != UserUID)
        {
            throw new Exception("Not host of room, cannot send");
        }

        // we are the host, so ensure that the UserUID we are sending it to is in RoomContextGroupVibeUsers
        if (!RoomContextGroupVibeUsers.TryGetValue(dto.RoomName, out var roomContextGroupVibeUsers))
        {
            throw new Exception("Room does not exist in the RoomContextGroupVibeUsers.");
        }

        // send the update to the user.
        await Clients.Users(dto.User).Client_PrivateRoomDeviceUpdate(dto).ConfigureAwait(false);
    }



    /// <summary> 
    /// Sends a vibe update to all connected vibe users except the host.
    /// </summary>
    public async Task PrivateRoomUpdateAllUserDevices(UpdateDeviceDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));
        // make sure we are the host of the room we are sending the update to.
        if (!RoomHosts.TryGetValue(dto.RoomName, out var roomHost) || roomHost != UserUID)
        {
            throw new Exception("Not host of room, cannot send");
        }

        // Use GroupExcept to send the update
        await Clients.OthersInGroup(dto.RoomName).Client_PrivateRoomDeviceUpdate(dto).ConfigureAwait(false);
    }

    // Leaving a room simply marks our user as false for inroom and updates the roomparticipants.
    public async Task PrivateRoomLeave(RoomParticipantDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));

        // Find the room the user is in
        var roomUser = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync(pru => pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);
        if (roomUser == null) return;

        // find the list of room participants associated with the same PrivateRoomNameID
        var roomParticipants = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == dto.RoomName)
            .ToListAsync()
            .ConfigureAwait(false);

        // mark us as false for InRoom & vibe access
        roomUser.InRoom = false;
        roomUser.AllowingVibe = false;
        // update our table
        DbContext.PrivateRoomPairs.Update(roomUser);
        // save changes
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // remove themselves from the vibe group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, dto.RoomName).ConfigureAwait(false);
        // if the user existed in the concurrent dictionary of contextgroupvibeusers, remove them.
        if (RoomContextGroupVibeUsers.TryGetValue(dto.RoomName, out var roomContextGroupVibeUsers))
        {
            roomContextGroupVibeUsers.Remove(UserUID);
        }

        // inform other room participants that we have become inactive in the room.
        var updatedPrivateRoomUser = new PrivateRoomUser
        {
            UserUID = dto.User.UserUID,
            ChatAlias = dto.User.ChatAlias,
            ActiveInRoom = false,
            VibeAccess = false
        };

        // push update to users
        await Clients.Users(roomParticipants.Select(u => u.PrivateRoomUserUID).ToList())
            .Client_PrivateRoomOtherUserLeft(new RoomParticipantDto(updatedPrivateRoomUser, dto.RoomName)).ConfigureAwait(false);
    }

    // Completely removes a room from existence. If you are the host calling this, it will remove the room.
    public async Task PrivateRoomRemove(string roomToRemove)
    {
        _logger.LogCallInfo();

        // Find the room the user is in
        var roomUser = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync
            (pru => pru.PrivateRoomUserUID == UserUID && pru.PrivateRoomNameID == roomToRemove).ConfigureAwait(false);

        if (roomUser == null) return;

        // check if the user is the host of the room they are calling this on
        var isHost = RoomHosts.TryGetValue(UserUID, out var roomName) && roomName == roomToRemove;

        // grab all participants in the room
        var roomParticipants = await DbContext.PrivateRoomPairs
            .Where(pru => pru.PrivateRoomNameID == roomToRemove)
            .ToListAsync()
            .ConfigureAwait(false);


        // if the user is NOT THE HOST, simply remove them from the PrivateRoom and respective Pair listing.
        if (!isHost)
        {
            // remove the privateRoomPair from the database associated with this room
            DbContext.PrivateRoomPairs.Remove(roomUser);
            // save changes
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // remove the user from the vibe group if they were in it.
            if (RoomContextGroupVibeUsers.TryGetValue(roomToRemove, out var roomContextGroupVibeUsers))
            {
                roomContextGroupVibeUsers.Remove(UserUID);
            }
            // get the respective connection id from the user
            if (_toyboxUserConnections.TryGetValue(UserUID, out var connectionId))
            {
                // attempt removing the user from the vibe group for the particular room.
                await Groups.RemoveFromGroupAsync(connectionId, roomToRemove).ConfigureAwait(false);
            }

            // Notify other participants that the user has been removed
            var updatedPrivateRoomUser = new PrivateRoomUser
            {
                UserUID = roomUser.PrivateRoomUserUID,
                ChatAlias = roomUser.ChatAlias,
                ActiveInRoom = false,
                VibeAccess = false
            };

            await Clients.Users(roomParticipants.Select(u => u.PrivateRoomUserUID).ToList())
                .Client_PrivateRoomRemovedUser(new RoomParticipantDto(updatedPrivateRoomUser, roomToRemove)).ConfigureAwait(false);
            // perform an early return.
            return;
        }
        else
        {
            // IF WE REACH HERE, THE HOST OF THE ROOM HAS CALLED THIS, SO WE MUST DESTROY THE ROOM

            // Inform all users that the room is closing
            var userUIDs = roomParticipants.Select(pru => pru.PrivateRoomUserUID).ToList();
            await Clients.Users(userUIDs).Client_PrivateRoomClosed(roomName).ConfigureAwait(false);

            // Batch remove all users from the group in the serverEndconnections.
            var tasks = userUIDs.Select(userUID => _toyboxUserConnections.TryGetValue(userUID, out var connectionId)
                    ? Groups.RemoveFromGroupAsync(connectionId, roomName) : Task.CompletedTask).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // remove the roomhost from the roomhost dictionary
            RoomHosts.TryRemove(UserUID, out _);

            // remove all PrivateRoomPairs associated with the room
            DbContext.PrivateRoomPairs.RemoveRange(roomParticipants);

            // remove the room itself
            var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == roomToRemove).ConfigureAwait(false);
            if (room != null)
            {
                DbContext.PrivateRooms.Remove(room);
            }

            // Save changes to the database
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }



    // Helper function that determines if the user is active in the room they are trying to communicate with.
    public async Task<bool> IsActiveInRoom(string roomName)
    {
        return await DbContext.PrivateRoomPairs.AnyAsync(
            pru => pru.PrivateRoomUserUID == UserUID &&
            pru.PrivateRoomNameID == roomName &&
            pru.InRoom
         ).ConfigureAwait(false);
    }

    // Same helper function but for any room at all.
    public async Task<bool> IsActiveInAnyRoom(string roomName)
        => await DbContext.PrivateRoomPairs.AnyAsync(pru => pru.PrivateRoomUserUID == UserUID && pru.InRoom).ConfigureAwait(false);

}

