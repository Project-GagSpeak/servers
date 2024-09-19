using GagspeakAPI.Enums;

namespace GagspeakShared.Utils;

/// <summary> Represents a message sent to the client. </summary>
public record HardReconnectMessage(MessageSeverity Severity, string Message, ServerState State);
