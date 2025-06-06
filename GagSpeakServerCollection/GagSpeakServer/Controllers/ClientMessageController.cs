﻿using GagspeakAPI.Hub;
using GagspeakServer.Hubs;
using GagspeakShared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

// Define the namespace for this controller.
// This controller is responsible for handling messages sent to clients.
namespace GagspeakServer.Controllers;

/// <summary>
/// Controller here allows us to trigger certain Client Callback calls via our discord bot.
/// </summary>
[Route("/msgc")]
[Authorize(Policy = "Internal")]
public class ClientMessageController : Controller
{
    // Declare private variables for logger and hub context
    private ILogger<ClientMessageController> _logger;
    private IHubContext<GagspeakHub, IGagspeakHub> _hubContextMain;

    // Constructor for this controller, injecting dependencies
    public ClientMessageController(ILogger<ClientMessageController> logger, IHubContext<GagspeakHub, IGagspeakHub> hubContext)
    {
        _logger = logger;
        _hubContextMain = hubContext;
    }

    // Define the route and HTTP method for sending a message
    [Route("sendMessage")]
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ClientMessage msg)
    {
        if(msg is null)
        {
            _logger.LogError("Received a null message");
            return Empty;
        }
        // Check if the message has a UID
        bool hasUid = !string.IsNullOrEmpty(msg.UID);

        // If no UID, send the message to all online users
        if (!hasUid)
        {
            _logger.LogInformation("Sending Message of severity {severity} to all online users: {message}", msg.Severity, msg.Message);
            await _hubContextMain.Clients.All.Callback_ServerMessage(msg.Severity, msg.Message).ConfigureAwait(false);
        }
        // If there is a UID, send the message to the specific user
        else
        {
            _logger.LogInformation("Sending Message of severity {severity} to user {uid}: {message}", msg.Severity, msg.UID, msg.Message);
            await _hubContextMain.Clients.User(msg.UID).Callback_ServerMessage(msg.Severity, msg.Message).ConfigureAwait(false);
        }

        // Return an empty result
        return Empty;
    }

    // Forces all users to reconnect to the main server. (fixing any prone internal reconnection errors.
    [Route("forceHardReconnect")]
    [HttpPost]
    public async Task<IActionResult> ForceHardReconnect([FromBody] HardReconnectMessage msg)
    {
        if(msg is null)
        {
            _logger.LogError("Received a null message");
            return Empty;
        }
        _logger.LogInformation("Sending Message of severity {severity} to all online users: {message}", msg.Severity, msg.Message);
        await _hubContextMain.Clients.All.Callback_HardReconnectMessage(msg.Severity, msg.Message, msg.State).ConfigureAwait(false);

        // Return an empty result
        return Empty;
    }
}