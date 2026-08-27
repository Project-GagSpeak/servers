using GagspeakAPI.Enums;

namespace GagspeakShared.Utils;

/// <summary> Represents a message sent to the client. </summary>
public record ClientMessage(MessageSeverity Severity, string Message, string UID);

/// <summary> Represents a message sent to the client. </summary>
public record HardReconnectMessage(MessageSeverity Severity, string Message, ServerState State, string userUID);

/// <summary> Non-Messagepack, Serializable ChatlogMessage for report serialization. </summary>
public readonly record struct JsonChatMsg(string MsgId, DateTime TimeSentUTC, string SenderUid, string SenderName, string Message);
