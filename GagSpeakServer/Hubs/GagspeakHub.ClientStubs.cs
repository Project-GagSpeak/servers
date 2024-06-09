// Import necessary namespaces
using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto;
using Gagspeak.API.Dto.User;
using System;

// Define the namespace for the hub
// 
// file is part of the GagspeakServer project in your workspace. It's located in the Hubs namespace, 
// suggesting it's used for managing real-time communication between the server and clients using SignalR.
namespace GagspeakServer.Hubs;

// Define the GagspeakHub class
// 
// Each method throws a PlatformNotSupportedException because these methods are placeholders
// for the client-side implementation and are not meant to be called on the server-side.
public partial class GagspeakHub
{
    // This method is called when the client receives a server message
    public Task Client_ReceiveServerMessage(MessageSeverity messageSeverity, string message)
        => throw new PlatformNotSupportedException("Calling clientside method on server not supported");

    // This method is called when the client updates system info
    public Task Client_UpdateSystemInfo(SystemInfoDto systemInfo)
        => throw new PlatformNotSupportedException("Calling clientside method on server not supported");
}
