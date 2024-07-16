using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Toybox;

namespace GagspeakServer.Hubs
{
    /// <summary> For client stubs </summary>
    public partial class ToyboxHub
    {
        public Task Client_ReceiveToyboxServerMessage(MessageSeverity messageSeverity, string message) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
        public Task Client_UpdateIntensity(byte newIntensity, bool wasApplying) => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
    }
}