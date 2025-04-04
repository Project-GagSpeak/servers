using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI;
using GagspeakAPI.Enums;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary> handles the hardcore adult immersive content. </summary>
public partial class GagspeakHub
{
    /// <summary> Attempts to create a room with the specified room name. </summary>
    /// <remarks> Will return false if the room name already exists or if it failed to create. </remarks>
    public async Task<GsApiVibeErrorCodes> RoomCreate(string roomName, string password)
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomCreate - Method not yet Implemented"
            + $"\nUnable to Create room [{roomName}] with password [{password}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Sends an invite to a user to join a room. </summary>
    public async Task<GsApiVibeErrorCodes> SendRoomInvite(VibeRoomInviteDto dto)
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "SendRoomInvite - Method not yet Implemented"
            + $"\nUnable to invite user [{dto.User.UID}] to room [{dto.RoomName}] with password [{dto.RoomPassword}] with message [{dto.AttachedMessage}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Changes the password of an existing room. </summary>
    public async Task<GsApiVibeErrorCodes> ChangeRoomPassword(string roomName, string newPassword)
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "ChangeRoomPassword - Method not yet Implemented"
            + $"\nUnable to change password for room [{roomName}] to [{newPassword}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }
    /// <summary> Allows a user to join the room. </summary>
    public async Task<List<VibeRoomKinksterFullDto>> RoomJoin(string roomName, string password, VibeRoomKinkster dto)
    {
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomJoin - Method not yet Implemented"
            + $"\nUnable to join room [{roomName}] with password [{password}].").ConfigureAwait(false);
        return new List<VibeRoomKinksterFullDto>();
    }

    /// <summary> Allows a user to leave the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomLeave()
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomLeave - Method not yet Implemented"
            + $"\nUnable to leave room.").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Grants access to a user in the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomGrantAccess(UserDto allowedUser)
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomGrantAccess - Method not yet Implemented"
            + $"\nUnable to grant access to user [{allowedUser.User.UID}] in room [{allowedUser.User.UID}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Revokes access from a user in the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomRevokeAccess(UserDto allowedUser)
    {
        // Was bool
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomRevokeAccess - Method not yet Implemented"
            + $"\nUnable to revoke access from user [{allowedUser.User.UID}] in room [{allowedUser.User.UID}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Pushes device update (e.g., for battery level, motor settings) to the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomPushDeviceUpdate(DeviceInfo deviceInfo)
    {
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomPushDeviceUpdate - Method not yet Implemented"
            + $"\nUnable to push device update for device [{deviceInfo.DeviceName}] in room [{deviceInfo.DeviceIndex}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Sends a data stream (vibration/rotation data) to users in the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomSendDataStream(SexToyDataStreamDto dataStream)
    {
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomSendDataStream - Method not yet Implemented"
            + $"\nUnable to send data stream to room [{dataStream.DataStream}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
    }

    /// <summary> Sends a chat message to the room. </summary>
    public async Task<GsApiVibeErrorCodes> RoomSendChat(string roomName, string message)
    {
        await Client_ReceiveServerMessage(MessageSeverity.Error, "RoomSendChat - Method not yet Implemented"
            + $"\nUnable to send chat message to room [{roomName}] with message [{message}].").ConfigureAwait(false);
        return GsApiVibeErrorCodes.MethodNotImplemented;
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

