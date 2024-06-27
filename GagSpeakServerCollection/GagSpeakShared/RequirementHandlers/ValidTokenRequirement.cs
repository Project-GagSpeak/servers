using Microsoft.AspNetCore.Authorization;

namespace GagspeakShared.RequirementHandlers;

/// <summary> A requirement for a valid token. </summary>
public class ValidTokenRequirement : IAuthorizationRequirement { }