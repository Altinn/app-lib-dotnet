using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
    {
        return typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            && typeInfo.IsDefined(typeof(ApiControllerAttribute))
            && typeof(ControllerBase).IsAssignableFrom(typeInfo);
    }
}
