using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto.User;

namespace GagspeakServer.Hubs
{
    /// <summary> For client stubs </summary>
    public partial class ToyboxHub
    {
        public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateIntensity(UserDto user, byte intensity) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}