using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.Connection;

namespace GagspeakServer.Hubs
{
    /// <summary> For client stubs </summary>
    public partial class ToyboxHub
    {
        public Task Client_ReceiveToyboxServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveRoomInvite(RoomInviteDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomJoined(RoomInfoDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomOtherUserJoined(RoomParticipantDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomOtherUserLeft(RoomParticipantDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomRemovedUser(RoomParticipantDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomUpdateUser(RoomParticipantDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomMessage(RoomMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomReceiveUserDevice(UserCharaDeviceInfoMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomDeviceUpdate(UpdateDeviceDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_PrivateRoomClosed(string roomName) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}