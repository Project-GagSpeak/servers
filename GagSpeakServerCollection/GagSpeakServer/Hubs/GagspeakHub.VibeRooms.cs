using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary> handles the hardcore adult immersive content. </summary>
public partial class GagspeakHub
{
    /// <summary> Attempts to create a room with the specified room name. </summary>
    /// <remarks> Will return false if the room name already exists or if it failed to create. </remarks>
    public async Task<HubResponse> RoomCreate(RoomCreateRequest dto)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomCreate - Method not yet Implemented"
            + $"\nUnable to Create room [{dto.Name}] with password [{dto.Password}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Sends an invite to a user to join a room. </summary>
    public async Task<HubResponse> SendRoomInvite(RoomInvite dto)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "SendRoomInvite - Method not yet Implemented"
            + $"\nUnable to invite user [{dto.User.UID}] to room [{dto.RoomName}] with password [{dto.RoomPassword}] with message [{dto.AttachedMessage}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Changes the password of an existing room. </summary>
    public async Task<HubResponse> ChangeRoomPassword(string name, string newPass)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "ChangeRoomPassword - Method not yet Implemented"
            + $"\nUnable to change password for room [{name}] to [{newPass}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }
    /// <summary> Allows a user to join the room. </summary>
    public async Task<HubResponse<List<RoomParticipant>>> RoomJoin(string name, string pass, RoomParticipantBase dto)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomJoin - Method not yet Implemented"
            + $"\nUnable to join room [{name}] with password [{pass}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt<List<RoomParticipant>>(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Allows a user to leave the room. </summary>
    public async Task<HubResponse> RoomLeave()
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomLeave - Method not yet Implemented\nUnable to leave room.").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Grants access to a user in the room. </summary>
    public async Task<HubResponse> RoomGrantAccess(KinksterBase allowedUser)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomGrantAccess - Method not yet Implemented"
            + $"\nUnable to grant access to user [{allowedUser.User.UID}] in room [{allowedUser.User.UID}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Revokes access from a user in the room. </summary>
    public async Task<HubResponse> RoomRevokeAccess(KinksterBase restrictedUser)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomRevokeAccess - Method not yet Implemented"
            + $"\nUnable to revoke access from user [{restrictedUser.User.UID}] in room [{restrictedUser.User.UID}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Pushes device update (e.g., for battery level, motor settings) to the room. </summary>
    public async Task<HubResponse> RoomPushDeviceUpdate(ToyInfo deviceInfo)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomPushDeviceUpdate - Method not yet Implemented"
            + $"\nUnable to push device update for device [{deviceInfo.Name}] in room [{deviceInfo.DeviceIdx}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Sends a data stream (vibration/rotation data) to users in the room. </summary>
    public async Task<HubResponse> RoomSendDataStream(ToyDataStream streamDto)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomSendDataStream - Method not yet Implemented"
            + $"\nUnable to send data stream to room.").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Sends a chat message to the room. </summary>
    public async Task<HubResponse> RoomSendChat(string name, string message)
    {
        await Callback_ServerMessage(MessageSeverity.Error, "RoomSendChat - Method not yet Implemented"
            + $"\nUnable to send chat message to room [{name}] with message [{message}].").ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
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

    // Same helper function but for any room at all. (Might be better to use redi's over this to be honest)
    public async Task<bool> IsActiveInAnyRoom(string roomName)
        => await DbContext.PrivateRoomPairs.AnyAsync(pru => pru.PrivateRoomUserUID == UserUID && pru.InRoom).ConfigureAwait(false);

}

