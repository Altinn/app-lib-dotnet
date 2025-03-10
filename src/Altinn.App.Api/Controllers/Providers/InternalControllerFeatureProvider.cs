using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        // https://source.dot.net/#Microsoft.AspNetCore.Mvc.Core/Controllers/ControllerFeatureProvider.cs,41
        // contains
        //
        // // We only consider public top-level classes as controllers. IsPublic returns false for nested
        // // classes, regardless of visibility modifiers
        // if (!typeInfo.IsPublic)
        // {
        //   return false;
        // }

        return typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            && typeInfo.IsDefined(typeof(ApiControllerAttribute))
            && typeof(ControllerBase).IsAssignableFrom(typeInfo);
    }
}
