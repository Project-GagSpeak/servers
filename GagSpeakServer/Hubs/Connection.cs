using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GagspeakServer.Hubs
{
    public class Connection : Hub
    {
        public string Heartbeat()
        {
            // get the user id from the context
            var userId = Context.User!.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            // if the user id is not null, then we can get the user
            if (userId != null)
            {
                // get the user
                var user = Clients.User(userId);
            }

            // return the user id, or an empty string if it is null
            return userId ?? string.Empty;
        }
    }
}