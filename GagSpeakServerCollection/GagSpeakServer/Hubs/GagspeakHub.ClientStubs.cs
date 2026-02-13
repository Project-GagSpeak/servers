using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Network;

namespace GagspeakServer.Hubs;

/// <summary>
///	The parts of the IGagspeakHub intended for the client-side, not the server side.
///	If any of these are called on the server-side, they will throw a PlatformNotSupportedException.
/// </summary>
public partial class GagspeakHub
{
	private const string UnsupportedMessage = "Calling Client-Side method on server not supported";
	public Task Callback_ServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_HardReconnectMessage(MessageSeverity messageSeverity, string message, ServerState state) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ServerInfo(ServerInfoResponse info) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_AddClientPair(KinksterPair dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemoveClientPair(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_AddPairRequest(KinksterRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemovePairRequest(KinksterRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_AddCollarRequest(CollarRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemoveCollarRequest(CollarRequest dto) => throw new PlatformNotSupportedException(UnsupportedMessage);

    public Task Callback_MoodleDataUpdated(MoodlesDataUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_MoodleSMUpdated(MoodlesSMUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_MoodleStatusesUpdate(MoodlesStatusesUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_MoodlePresetsUpdate(MoodlesPresetsUpdate dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_MoodleStatusModified(MoodlesStatusModified dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_MoodlePresetModified(MoodlesPresetModified dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_ApplyMoodlesByGuid(ApplyMoodleId dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ApplyMoodlesByStatus(ApplyMoodleStatus dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RemoveMoodles(RemoveMoodleId dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ClearMoodles(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_BulkChangeGlobal(BulkChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_BulkChangeUnique(BulkChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SingleChangeGlobal(SingleChangeGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_SingleChangeUnique(SingleChangeUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_SingleChangeAccess(SingleChangeAccess dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_StateChangeHardcore(HardcoreStateChange dto) => throw new PlatformNotSupportedException(UnsupportedMessage);


    public Task Callback_KinksterUpdateComposite(KinksterUpdateComposite dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveGag(KinksterUpdateActiveGag dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveRestriction(KinksterUpdateActiveRestriction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveRestraint(KinksterUpdateActiveRestraint dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveCollar(KinksterUpdateActiveCollar dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActiveCursedLoot(KinksterUpdateActiveCursedLoot dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateAliasState(KinksterUpdateAliasState dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveAliases(KinksterUpdateActiveAliases dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateValidToys(KinksterUpdateValidToys dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterUpdateActivePattern(KinksterUpdateActivePattern dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveAlarms(KinksterUpdateActiveAlarms dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateActiveTriggers(KinksterUpdateActiveTriggers dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ListenerName(SendNameAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ShockInstruction(ShockCollarAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_HypnoticEffect(HypnoticAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    
	public Task Callback_KinksterNewGagData(KinksterNewGagData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestrictionData(KinksterNewRestrictionData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewRestraintData(KinksterNewRestraintData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterNewCollarData(KinksterNewCollarData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewLootData(KinksterNewLootData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
    public Task Callback_KinksterNewAliasData(KinksterNewAliasData dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
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