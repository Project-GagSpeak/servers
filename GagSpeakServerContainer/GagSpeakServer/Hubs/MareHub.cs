using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

// GagSpeakHub.cs
public class GagSpeakHub : Hub<IGagSpeakHub>
{
    public async Task<string> Connect()
    {
        // Simulate some work with a delay
        await Task.Delay(1000);

        return "Connected";
    }

    public async Task<string> Disconnect()
    {
        // Simulate some work with a delay
        await Task.Delay(1000);

        return "Disconnected";
    }
}