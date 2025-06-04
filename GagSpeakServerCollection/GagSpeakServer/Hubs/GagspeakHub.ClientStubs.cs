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
	public Task Callback_KinksterUpdateIpc(KinksterUpdateIpc dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateGagSlot(KinksterUpdateGagSlot dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateRestriction(KinksterUpdateRestriction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateRestraint(KinksterUpdateRestraint dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateCursedLoot(KinksterUpdateCursedLoot dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateAliasGlobal(KinksterUpdateAliasGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateAliasUnique(KinksterUpdateAliasUnique dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateToybox(KinksterUpdateToybox dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterUpdateLightStorage(KinksterUpdateLightStorage dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ListenerName(UserData user, string name) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ShockInstruction(ShockCollarAction dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_ChatMessageGlobal(ChatMessageGlobal dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterOffline(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_KinksterOnline(OnlineKinkster dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ProfileUpdated(KinksterBase dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_ShowVerification(VerificationCode dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
		
	public Task Callback_RoomJoin(RoomParticipant dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomLeave(RoomParticipant dto) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomDeviceUpdate(UserData user, ToyInfo ToyInfo) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomIncDataStream(ToyDataStreamResponse dataStream) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomAccessGranted(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomAccessRevoked(UserData user) => throw new PlatformNotSupportedException(UnsupportedMessage);
	public Task Callback_RoomChatMessage(UserData user, string message) => throw new PlatformNotSupportedException(UnsupportedMessage);
}