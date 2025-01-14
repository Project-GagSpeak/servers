using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Enums;

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
        public Task Client_ReceiveHardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState state) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemoveMoodles(RemoveMoodlesDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserClearMoodles(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserAddClientPair(UserPairDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemoveClientPair(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserAddPairRequest(UserPairRequestDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserRemovePairRequest(UserPairRequestDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserUpdateAllPerms(UserPairUpdateAllPermsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateAllGlobalPerms(UserPairUpdateAllGlobalPermsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateAllUniquePerms(UserPairUpdateAllUniqueDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdatePairPermsGlobal(UserGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdatePairPerms(UserPairPermChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdatePairPermAccess(UserPairAccessChangeDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserReceiveDataComposite(OnlineUserCompositeDataDto dataDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataIpc(OnlineUserCharaIpcDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataAppearance(OnlineUserCharaAppearanceDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataWardrobe(OnlineUserCharaWardrobeDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataOrders(OnlineUserCharaOrdersDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataAlias(OnlineUserCharaAliasDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveDataToybox(OnlineUserCharaToyboxDataDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserReceiveLightStorage(OnlineUserStorageUpdateDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        public Task Client_UserReceiveShockInstruction(ShockCollarActionDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_GlobalChatMessage(GlobalChatMessageDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOffline(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserSendOnline(OnlineUserIdentDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UserUpdateProfile(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_DisplayVerificationPopup(VerificationDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}