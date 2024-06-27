using Microsoft.AspNetCore.Authorization;

namespace GagspeakShared.RequirementHandlers;

/// <summary> A base UserRequirement class, containing a list of user requirements. </summary>
public class UserRequirement : IAuthorizationRequirement
{
    public UserRequirement(UserRequirements requirements)
    {
        Requirements = requirements;
    }

    public UserRequirements Requirements { get; }
}
