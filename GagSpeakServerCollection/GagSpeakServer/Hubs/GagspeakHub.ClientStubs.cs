using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Network;

namespace GagspeakServer.Hubs;

/// <summary>
///	The parts of the IGagspeakHub intended for the client-side, not the serverside.
///	If any of these are called on the server-side, they will throw a PlatformNotSupportedException.
/// </summary>
public partial class GagspeakHub
{
	private const string UnsupportedMessage = "Calling Client-Side method on server not supported";
	public Task Callback_ServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_HardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState state) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ServerInfo(ServerInfoResponse info) => throw new PlatformNotSupportedException(UnsupportedMessage)
		;
	public Task Callback_AddClientPair(KinksterPair dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemoveClientPair(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_AddPairRequest(KinksterRequestEntry dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemovePairRequest(KinksterRequestEntry dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_SetKinksterIpcFull(KinksterIpcDataFull dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SetKinksterIpcStatusManager(KinksterIpcStatusManager dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SetKinksterIpcStatuses(KinksterIpcStatuses dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SetKinksterIpcPresets(KinksterIpcPresets dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ApplyMoodlesByGuid(MoodlesApplierById dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ApplyMoodlesByStatus(MoodlesApplierByStatus dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemoveMoodles(MoodlesRemoval dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ClearMoodles(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_BulkChangeAll(BulkChangeAll dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_BulkChangeUnique(BulkChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_SingleChangeUnique(SingleChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SingleChangeAccess(SingleChangeAccess dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveCursedLoot(KinksterUpdateActiveCursedLoot dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateAliasGlobal(KinksterUpdateAliasGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateAliasUnique(KinksterUpdateAliasUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateValidToys(KinksterUpdateValidToys dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActivePattern(KinksterUpdateActivePattern dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveAlarms(KinksterUpdateActiveAlarms dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveTriggers(KinksterUpdateActiveTriggers dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ListenerName(UserData user, string name) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ShockInstruction(ShockCollarAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_HypnoticEffect(HypnoticAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ConfineToAddress(ConfineByAddress dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ImprisonAtPosition(ImprisonAtPosition dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    
	public Task Callback_KinksterNewGagData(KinksterNewGagData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewLootData(KinksterNewLootData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewPatternData(KinksterNewPatternData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterNewAlarmData(KinksterNewAlarmData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterNewTriggerData(KinksterNewTriggerData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterNewAllowances(KinksterNewAllowances dto) => throw new PlatformNotSupportedException(UnsupportedMessage);

    public Task Callback_ChatMessageGlobal(ChatMessageGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterOffline(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterOnline(OnlineKinkster dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ProfileUpdated(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ShowVerification(VerificationCode dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_RoomJoin(RoomParticipant dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomLeave(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomAddInvite(RoomInvite dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomHostChanged(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo ToyInfo) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomIncDataStream(ToyDataStreamResponse dataStream) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomAccessGranted(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomAccessRevoked(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomChatMessage(UserData user, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
}