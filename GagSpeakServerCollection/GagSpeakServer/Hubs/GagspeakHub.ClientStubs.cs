using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto.User;
using GagSpeak.API.Dto.Connection;
using GagSpeak.API.Dto.Permissions;
using GagSpeak.API.Dto.UserPair;

namespace GagspeakServer.Hubs
{
    /// <summary> This section of the partial class for GagspeakHub contains the client stubs.
    /// <para> Client stubs are the functions that the server can call upon from its connected clients.</para>
    /// <para>
    /// This means that the clients should never be able to call these functions on the server.
    /// If they do try, we will throw an exception for each of these methods.
    /// </para>
    /// </summary>
    public partial class GagspeakHub
    {
        public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserAddClientPair(UserPairDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemoveClientPair(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateUserIndividualPairStatusDto(UserIndividualPairStatusDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveCharacterData(OnlineUserCharaCompositeDataDto dataDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOffline(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOnline(OnlineUserIdentDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateProfile(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_DisplayVerificationPopup(VerificationDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}