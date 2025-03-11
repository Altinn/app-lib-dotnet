using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    private const string ControllerTypeNameSuffix = "Controller";

    protected override bool IsController(TypeInfo typeInfo)
    {
        if (!typeInfo.IsClass)
        {
            return false;
        }

        if (typeInfo.IsAbstract)
        {
            return false;
        }

        if (typeInfo.ContainsGenericParameters)
        {
            return false;
        }

        if (typeInfo.IsDefined(typeof(NonControllerAttribute)))
        {
            return false;
        }

        if (
            !typeInfo.Name.EndsWith(ControllerTypeNameSuffix, StringComparison.OrdinalIgnoreCase)
            && !typeInfo.IsDefined(typeof(ControllerAttribute))
        )
        {
            return false;
        }

        if (!typeof(ControllerBase).IsAssignableFrom(typeInfo))
        {
            return false;
        }

        return true;
    }
}
