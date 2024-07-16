using GagspeakAPI.Data.Enum;

namespace GagspeakShared.Utils;

/// <summary> Represents a message sent to the client. </summary>
public record ClientMessage(MessageSeverity Severity, string Message, string UID);
