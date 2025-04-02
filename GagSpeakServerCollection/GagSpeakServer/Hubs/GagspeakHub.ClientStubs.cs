using GagspeakAPI.Data;
using GagspeakAPI.Dto;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Dto.VibeRoom;
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
        public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_ReceiveHardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState state) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        public Task Client_UserAddClientPair(UserPairDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserRemoveClientPair(UserDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserAddPairRequest(UserPairRequestDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserRemovePairRequest(UserPairRequestDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        /// <summary> Callbacks to update moodles. </summary>
        public Task Client_UserApplyMoodlesByGuid(ApplyMoodlesByGuidDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserApplyMoodlesByStatus(ApplyMoodlesByStatusDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserRemoveMoodles(RemoveMoodlesDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserClearMoodles(UserDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        /// <summary> Callbacks to update permissions. </summary>
        public Task Client_UserUpdateAllPerms(BulkUpdatePermsAllDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserUpdateAllGlobalPerms(BulkUpdatePermsGlobalDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserUpdateAllUniquePerms(BulkUpdatePermsUniqueDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserUpdatePairPermsGlobal(UserGlobalPermChangeDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserUpdatePairPerms(UserPairPermChangeDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        public Task Client_UserUpdatePairPermAccess(UserPairAccessChangeDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        /// <summary> Callbacks to update own or pair data. </summary>
        public Task Client_UserReceiveDataComposite(OnlineUserCompositeDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataIpc(CallbackIpcDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataGags(CallbackGagDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataRestrictions(CallbackRestrictionDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataRestraint(CallbackRestraintDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataCursedLoot(CallbackCursedLootDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataOrders(CallbackOrdersDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataAlias(CallbackAliasDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveDataToybox(CallbackToyboxDataDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveLightStorage(CallbackLightStorageDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserReceiveShockInstruction(ShockCollarActionDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        public Task Client_RoomJoin(VibeRoomKinksterFullDto user) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomLeave(VibeRoomKinksterFullDto user) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomReceiveDeviceUpdate(UserData user, DeviceInfo deviceInfo) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomReceiveDataStream(SexToyDataStreamCallbackDto dataStream) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomUserAccessGranted(UserData user) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomUserAccessRevoked(UserData user) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_RoomReceiveChatMessage(UserData user, string message) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        public Task Client_GlobalChatMessage(GlobalChatMessageDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserSendOffline(UserDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserSendOnline(OnlineUserIdentDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
        public Task Client_UserUpdateProfile(UserDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");

        public Task Client_DisplayVerificationPopup(VerificationDto dto) => throw new PlatformNotSupportedException("Calling Client-Side method on server not supported");
    }
}