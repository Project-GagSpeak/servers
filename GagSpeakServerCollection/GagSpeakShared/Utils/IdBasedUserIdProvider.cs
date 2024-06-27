using Microsoft.AspNetCore.SignalR;

namespace GagspeakShared.Utils;

/// <summary> A helper class for providing user IDs based on the user's ID via the claim type. </summary>
public class IdBasedUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext context)
    {
        return context.User!.Claims.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value;
    }
}
