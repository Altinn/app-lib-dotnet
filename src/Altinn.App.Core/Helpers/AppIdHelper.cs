using System.Diagnostics.CodeAnalysis;

namespace Altinn.App.Core.Helpers;

internal sealed class AppIdHelper
{
    internal static string ToResourceId(string appId)
    {
        return ""; //TODO
    }

    internal static bool IsResourceId(string appId)
    {
        return false; //TODO
    }

    internal static bool TryGetResourceId(string appId, [NotNullWhen(true)] out string? resourceId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            resourceId = null;
            return false;
        }

        if (IsResourceId(appId))
        {
            resourceId = appId;
            return true;
        }

        resourceId = ToResourceId(appId);

        if (IsResourceId(resourceId))
        {
            return true;
        }
        else
        {
            resourceId = null;
            return false;
        }
    }
}
