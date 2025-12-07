using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace GagspeakShared.Utils;

/// <summary> The feature provider for our server controllers </summary>
public class AllowedControllersFeatureProvider : ControllerFeatureProvider
{
    private readonly Type[] _allowedTypes;

    public AllowedControllersFeatureProvider(params Type[] allowedTypes)
    {
        _allowedTypes = allowedTypes;
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        return base.IsController(typeInfo) && _allowedTypes.Contains(typeInfo.AsType());
    }
}
