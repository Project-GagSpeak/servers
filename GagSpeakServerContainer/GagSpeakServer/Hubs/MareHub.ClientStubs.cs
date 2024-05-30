// Import necessary namespaces
using GagSpeak.API.Data.Enum;
using GagSpeak.API.Dto;
using GagSpeak.API.Dto.Group;
using GagSpeak.API.Dto.User;
using System;

// Define the namespace for the hub
// 
// file is part of the GagSpeakServer project in your workspace. It's located in the Hubs namespace, 
// suggesting it's used for managing real-time communication between the server and clients using SignalR.
namespace GagSpeakServer.Hubs
{
    // Define the GagSpeakHub class
    // 
    // Each method throws a PlatformNotSupportedException because these methods are placeholders
    // for the client-side implementation and are not meant to be called on the server-side.
    public partial class GagSpeakHub
    {
        // This method is called when the client is ready to download data
        public Task Client_DownloadReady(Guid requestId) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client changes group permissions
        public Task Client_GroupChangePermissions(GroupPermissionDto groupPermission) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client deletes a group
        public Task Client_GroupDelete(GroupDto groupDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client changes user info in a group pair
        public Task Client_GroupPairChangeUserInfo(GroupPairUserInfoDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client joins a group pair
        public Task Client_GroupPairJoined(GroupPairFullInfoDto groupPairInfoDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client leaves a group pair
        public Task Client_GroupPairLeft(GroupPairDto groupPairDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client sends full info about a group
        public Task Client_GroupSendFullInfo(GroupFullInfoDto groupInfo) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client sends info about a group
        public Task Client_GroupSendInfo(GroupInfoDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client receives a server message
        public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates system info
        public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client adds a client pair to a user
        public Task Client_UserAddClientPair(UserPairDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client receives character data for a user
        public Task Client_UserReceiveCharacterData(OnlineUserCharaDataDto dataDto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client receives upload status for a user
        public Task Client_UserReceiveUploadStatus(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client removes a client pair from a user
        public Task Client_UserRemoveClientPair(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client sends offline status for a user
        public Task Client_UserSendOffline(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client sends online status for a user
        public Task Client_UserSendOnline(OnlineUserIdentDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates other pair permissions for a user
        public Task Client_UserUpdateOtherPairPermissions(UserPermissionsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates a user's profile
        public Task Client_UserUpdateProfile(UserDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates self pair permissions for a user
        public Task Client_UserUpdateSelfPairPermissions(UserPermissionsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates default permissions for a user
        public Task Client_UserUpdateDefaultPermissions(DefaultPermissionsDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client updates individual pair status for a user
        public Task Client_UpdateUserIndividualPairStatusDto(UserIndividualPairStatusDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

        // This method is called when the client changes user pair permissions in a group
        public Task Client_GroupChangeUserPairPermissions(GroupPairUserPermissionDto dto) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}
}