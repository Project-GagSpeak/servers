using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Toybox;

namespace GagspeakServer.Hubs
{
    /// <summary> For client stubs </summary>
    public partial class ToyboxHub
    {
        public Task Client_ReceiveToyboxServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveRoomInvite(RoomInviteDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserJoinedRoom(RoomInfoDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_OtherUserJoinedRoom(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_OtherUserLeftRoom(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveRoomMessage(RoomMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDeviceInfo(UserCharaDeviceInfoMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserDeviceUpdate(UpdateDeviceDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_ReceiveRoomClosedMessage(string roomName) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}