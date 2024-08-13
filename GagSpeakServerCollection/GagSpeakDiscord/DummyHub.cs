using Microsoft.AspNetCore.SignalR;

#pragma warning disable IDE0130
#pragma warning disable MA0048


// TODO: Potentially can remove this completely. See if we can after everything else is working and functional to ensure sucess.
namespace GagspeakServer.Hubs;
public class GagspeakHub : Hub
{
    public override Task OnConnectedAsync()
    {
        throw new NotSupportedException();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        throw new NotSupportedException();
    }
}
#pragma warning restore IDE0130
#pragma warning restore MA0048