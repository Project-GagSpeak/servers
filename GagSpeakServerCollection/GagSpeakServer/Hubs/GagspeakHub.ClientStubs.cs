using GagspeakAPI.Chat;
using GagspeakAPI.Connection;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Network;
using GagspeakAPI.Reporting;
using GagspeakAPI.User;

namespace GagspeakServer.Hubs;

/// <summary>
///	  The parts of the IGagspeakHub intended for the client-side, not the server side.
///	  If any of these are called on the server-side, they will throw a PlatformNotSupportedException.
/// </summary>
public partial class GagspeakHub
{
    private const string UnsupportedMessage = "Calling Client-Side method on server not supported";

    #region Callbacks (Connection Responses)
    public Task Callback_ServerMessage(MessageSeverity severity, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_HardReconnectMessage(MessageSeverity severity, string message, ServerState state) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_UserFlaggedForReport(ReportKind kind, string flaggedUID) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ReputationInfo(UserReputation reputation, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ServerInfo(ServerInfoResponse info) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (Pairs/Requests)
    public Task Callback_AddPair(KinksterPair dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RemovePair(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_PersistPair(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_AddPairRequest(KinksterRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RemovePairRequest(KinksterRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_AddCollarRequest(CollarRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RemoveCollarRequest(CollarRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (Locis)
    public Task Callback_LociDataUpdated(LociDataUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociStatusesUpdate(LociStatusesUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociPresetsUpdate(LociPresetsUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociStatusModified(LociStatusModified dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociPresetModified(LociPresetModified dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociApplyDataById(ApplyLociDataById dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociApplyStatus(ApplyLociStatus dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociRemoveData(RemoveLociData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_LociClearData(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (Permissions)
    public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_BulkChangeUnique(BulkChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_SingleChangeUnique(SingleChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_SingleChangeAccess(SingleChangeAccess dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_StateChangeHardcore(HardcoreStateChange dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (Data Updates)
    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActiveCollar(KinksterUpdateActiveCollar dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledItem(KinksterChangeEnabledItem dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledGag(KinksterChangeEnabledGag dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledToy(KinksterChangeEnabledToy dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledItems(KinksterChangeEnabledItems dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledGags(KinksterChangeEnabledGags dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterChangeEnabledToys(KinksterChangeEnabledToys dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ListenerName(SendNameAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ShockInstruction(ShockCollarAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_HypnoticEffect(HypnoticAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (Light Storage Updates)
    public Task Callback_KinksterNewGagData(KinksterNewGagData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewCollarData(KinksterNewCollarData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewLootData(KinksterNewLootData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewAliasData(KinksterNewAliasData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewPatternData(KinksterNewPatternData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewAlarmData(KinksterNewAlarmData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewTriggerData(KinksterNewTriggerData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region Callbacks (UserState / Misc.)
    public Task Callback_KinksterOnline(OnlineKinkster dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterOffline(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_UserVanityUpdated(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_UserProfileUpdated(UserDto dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ShowVerification(VerificationCode dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ChatMessageReceived(ChatlogMessage dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion

    #region VibeRooms
    public Task Callback_RoomJoin(RoomParticipant dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomLeave(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomAddInvite(RoomInvite dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomHostChanged(UserData newHost) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo ToyInfo) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomIncDataStream(ToyDataStreamResponse dataStream) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomAccessGranted(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomAccessRevoked(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomChatMessage(UserData user, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
    #endregion
}