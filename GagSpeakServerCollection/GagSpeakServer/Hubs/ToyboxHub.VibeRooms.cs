using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.User;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace GagspeakServer.Hubs;

/// <summary>
/// Partial class dealing with the main toybox hub room / group functionality.
/// </summary>
public partial class ToyboxHub
{
    // Key: PrivateRoomNameID, Value: HashSet of UserUIDs
    private static readonly ConcurrentDictionary<string, HashSet<string>> Rooms = new(StringComparer.Ordinal);
    // Key: PrivateRoomNameID, Value: Host UserUID
    private static readonly ConcurrentDictionary<string, string> RoomHosts = new(StringComparer.Ordinal);
    /// <summary> Create a new room. </summary>
    public async Task UserCreateNewRoom(RoomCreateDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));

        // Check if the room name already exists in the database
        var roomExists = DbContext.PrivateRooms.Any(r => r.NameID == dto.NewRoomName);
        if(roomExists)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage
                (MessageSeverity.Warning, "Room already in use.").ConfigureAwait(false);
            return;
        }

        // create a new private room
        var newRoom = new PrivateRoom
        {
            NameID = dto.NewRoomName,
            HostUID = UserUID,
            TimeMade = DateTime.UtcNow
        };
        // add the room to the database
        DbContext.PrivateRooms.Add(newRoom);

        // add the host as the private room user
        var newRoomUser = new PrivateRoomPair
        {
            PrivateRoomNameID = dto.NewRoomName,
            PrivateRoomUserUID = UserUID,
            ChatAlias = "Room Host"
        };
        // add the user to the database
        DbContext.PrivateRoomPairs.Add(newRoomUser);

        // save the changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Add the user to the SignalR Group
        await Groups.AddToGroupAsync(Context.ConnectionId, dto.NewRoomName).ConfigureAwait(false);
        await Clients.Caller.Client_ReceiveToyboxServerMessage
            (MessageSeverity.Information, $"Room {dto.NewRoomName} created.").ConfigureAwait(false);
    }

    public async Task UserRoomInvite(RoomInviteDto dto)
    {
        // check if the user inviting is the host of the room.
        var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == dto.RoomName).ConfigureAwait(false);
        if (room == null || room.HostUID != UserUID) return;

        // grab the caller from the db
        var user = await DbContext.Users.SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if(user == null) return;
        // send the invite to the user.
        await Clients.User(dto.UserInvited.UID)
            .Client_UserReceiveRoomInvite(new RoomInviteDto(user.ToUserData(), dto.RoomName)).ConfigureAwait(false);
    }

    public async Task UserJoinRoom(string roomName)
    {
        // Ensure the user is not already in another room
        var existingRoomUser = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync(pru => pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);
        if (existingRoomUser != null)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage
                (MessageSeverity.Error, $"You are already in a room ({existingRoomUser.PrivateRoomNameID}). "+
                "Leave the current room before joining another.").ConfigureAwait(false);
            return;
        }

        // Check if the room exists
        var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == roomName).ConfigureAwait(false);
        if (room == null)
        {
            await Clients.Caller.Client_ReceiveToyboxServerMessage
                (MessageSeverity.Error, $"Room {roomName} does not exist.").ConfigureAwait(false);
            return;
        }

        // Add the user to the room
        var newRoomUser = new PrivateRoomPair
        {
            PrivateRoomNameID = roomName,
            PrivateRoomUserUID = UserUID,
            ChatAlias = "Anonymous Kinkster"
        };
        DbContext.PrivateRoomPairs.Add(newRoomUser);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Add the user to the SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName).ConfigureAwait(false);
        await Clients.OthersInGroup(roomName).Client_ReceiveToyboxServerMessage
            (MessageSeverity.Information, $"{newRoomUser.ChatAlias} has joined the room.").ConfigureAwait(false);

        // grab our user from db
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user == null) return;

        // grab the list of users connected to the room
        var roomUsers = await DbContext.PrivateRoomPairs.Where(pru => pru.PrivateRoomNameID == roomName).Select(pru => pru.PrivateRoomUserUID).ToListAsync().ConfigureAwait(false);

        // send an update to other connected group clients that a new user has joined the room.
        await Clients.OthersInGroup(roomName).Client_OtherUserJoinedRoom(new UserDto(user.ToUserData())).ConfigureAwait(false);
        // send update to the client that they have joined the room.
        await Clients.Caller.Client_UserJoinedRoom(new RoomInfoDto(roomName, room.HostUID, roomUsers)).ConfigureAwait(false);
    }

    /// <summary> Send a message to the users in the room you are in. </summary>
    public async Task UserSendMessageToRoom(RoomMessageDto dto)
    {
        // Check if the user is in the room
        var roomUser = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync
            (pru => pru.PrivateRoomNameID == dto.RoomName && pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);

        if (roomUser == null) return;

        // Send the message to everyone in the group
        await Clients.Group(dto.RoomName).Client_UserReceiveRoomMessage(dto).ConfigureAwait(false);
    }


    /// <summary> Update the device of a particular user with new instructions . </summary>
    public async Task UserUpdateDevice(UpdateDeviceDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));
        // see if the person calling the order is the host of the room they are calling it for
        if (!RoomHosts.TryGetValue(dto.RoomName, out var roomHost) || roomHost != UserUID) return;
        // if the room does not exist, return.
        if (!Rooms.ContainsKey(dto.RoomName)) return;

        // update the user device with the new instructions.
        await Clients.Users(dto.User.UID).Client_UserDeviceUpdate(dto).ConfigureAwait(false);
    }



    /// <summary> Update the devices of a group of users with new instructions. </summary>
    public async Task UserUpdateGroupDevices(UpdateDeviceDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));
        // see if the person calling the order is the host of the room they are calling it for
        if (!RoomHosts.TryGetValue(dto.RoomName, out var roomHost) || roomHost != UserUID) return;
        // if the room does not exist, return.
        if (!Rooms.ContainsKey(dto.RoomName)) return;

        // update all other users in the room with the new instructions
        await Clients.OthersInGroup(dto.RoomName).Client_UserDeviceUpdate(dto).ConfigureAwait(false);
    }

    public async Task UserLeaveRoom()
    {
        _logger.LogCallInfo();

        // Find the room the user is in
        var roomUser = await DbContext.PrivateRoomPairs.FirstOrDefaultAsync(pru => pru.PrivateRoomUserUID == UserUID).ConfigureAwait(false);
        if (roomUser == null) return;

        var roomName = roomUser.PrivateRoomNameID;
        var room = await DbContext.PrivateRooms.FirstOrDefaultAsync(r => r.NameID == roomName).ConfigureAwait(false);

        // If the user is the host, delete the room
        if (room != null && string.Equals(room.HostUID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Group(roomName).Client_ReceiveRoomClosedMessage(roomName).ConfigureAwait(false);
            DbContext.PrivateRooms.Remove(room);
            DbContext.PrivateRoomPairs.RemoveRange(DbContext.PrivateRoomPairs.Where(pru => pru.PrivateRoomNameID == roomName));
        }
        else
        {
            // Remove the user from the room
            DbContext.PrivateRoomPairs.Remove(roomUser);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName).ConfigureAwait(false);

            await Clients.Group(roomName).Client_OtherUserLeftRoom
                (new UserDto(roomUser.PrivateRoomUser.ToUserData())).ConfigureAwait(false);

            await Clients.OthersInGroup(roomName).Client_ReceiveToyboxServerMessage
                (MessageSeverity.Information, $"{UserUID} has left the room.").ConfigureAwait(false);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UserPushDeviceInfo(UserCharaDeviceInfoMessageDto dto)
    {
        _logger.LogCallInfo(ToyboxHubLogger.Args(dto));
        // see if valid for the room they are sending info to
        if (!Rooms.ContainsKey(dto.RoomName) || !Rooms[dto.RoomName].Contains(UserUID)) return;

        // if valid, send the device info to the room.
        await Clients.OthersInGroup(dto.RoomName).Client_UserReceiveDeviceInfo(dto).ConfigureAwait(false);
    }
}

