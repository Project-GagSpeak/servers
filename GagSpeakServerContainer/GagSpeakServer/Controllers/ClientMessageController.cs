// Import necessary namespaces
using GagSpeak.API.Data.Enum;
using GagSpeak.API.SignalR;
using GagSpeakServer.Hubs;
using GagSpeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

// Define the namespace for this controller.
// This controller is responsible for handling messages sent to clients.
namespace GagSpeakServer.Controllers;

// Define the route and authorization policy for this controller
[Route("/msgc")]
[Authorize(Policy = "Internal")]
public class ClientMessageController : Controller
{
    // Declare private variables for logger and hub context
    private ILogger<ClientMessageController> _logger;
    private IHubContext<GagSpeakHub, IGagSpeakHub> _hubContext;

    // Constructor for this controller, injecting dependencies
    public ClientMessageController(ILogger<ClientMessageController> logger, IHubContext<GagSpeakHub, IGagSpeakHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    // Define the route and HTTP method for sending a message
    [Route("sendMessage")]
    [HttpPost]
    public async Task<IActionResult> SendMessage(ClientMessage msg)
    {
        // Check if the message has a UID
        bool hasUid = !string.IsNullOrEmpty(msg.UID);

        // If no UID, send the message to all online users
        if (!hasUid)
        {
            _logger.LogInformation("Sending Message of severity {severity} to all online users: {message}", msg.Severity, msg.Message);
            await _hubContext.Clients.All.Client_ReceiveServerMessage(msg.Severity, msg.Message).ConfigureAwait(false);
        }
        // If there is a UID, send the message to the specific user
        else
        {
            _logger.LogInformation("Sending Message of severity {severity} to user {uid}: {message}", msg.Severity, msg.UID, msg.Message);
            await _hubContext.Clients.User(msg.UID).Client_ReceiveServerMessage(msg.Severity, msg.Message).ConfigureAwait(false);
        }

        // Return an empty result
        return Empty;
    }
}