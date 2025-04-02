using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GagspeakShared.Utils;
using System.Globalization;

namespace GagspeakShared.RequirementHandlers;

/// <summary> The handler class for valid token requirement </summary>
public class ValidTokenRequirementHandler : AuthorizationHandler<ValidTokenRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ValidTokenRequirement requirement)
    {
        // Get the expiration claim value
        var expirationClaimValue = context.User.Claims.SingleOrDefault(r => string.Equals(r.Type, GagspeakClaimTypes.Expires, StringComparison.Ordinal));
        // if the expiration claim value is null, fail the context
        if (expirationClaimValue is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // otherwise, parse the expiration date
        DateTime expirationDate = new(long.Parse(expirationClaimValue.Value, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        // if the expiration date is less than the current date, fail the context
        if (expirationDate < DateTime.UtcNow)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // otherwise, succeed the context
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary> The handler class for valid token requirement for the hub. </summary>
public class ValidTokenHubRequirementHandler : AuthorizationHandler<ValidTokenRequirement, HubInvocationContext>
{
    /// <summary> Handles the requirement for the user. </summary>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ValidTokenRequirement requirement, HubInvocationContext resource)
    {
        // Get the expiration claim value
        var expirationClaimValue = context.User.Claims.SingleOrDefault(r => string.Equals(r.Type, GagspeakClaimTypes.Expires, StringComparison.Ordinal));
        // if the expiration claim value is null, fail the context
        if (expirationClaimValue is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // otherwise, parse the expiration date
        DateTime expirationDate = new(long.Parse(expirationClaimValue.Value, CultureInfo.InvariantCulture), DateTimeKind.Utc);
        // if the expiration date is less than the current date, fail the context
        if (expirationDate < DateTime.UtcNow)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // otherwise, succeed the context
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}