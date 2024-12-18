using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Helpers;

internal sealed class AppIdHelper
{
    internal static bool TryGetResourceId(string appId, [NotNullWhen(true)] out AppResourceId? resourceId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            resourceId = null;
            return false;
        }

        AppResourceId appResourceId = new(appId);

        if (AppResourceId.IsResourceId(appResourceId))
        {
            resourceId = appResourceId;
            return true;
        }
        else
        {
            resourceId = null;
            return false;
        }
    }
}

internal partial class AppResourceId
{
    [GeneratedRegex("app_[a-zA-Z0-9]+_[a-zA-Z0-9]+")]
    private static partial Regex AppIdRegex();

    internal AppResourceId(string org, string app)
    {
        Org = org;
        App = app;
    }

    internal AppResourceId(string appId)
    {
        string[] appIdParts = appId.Split('/');
        Org = appIdParts[0];
        App = appIdParts[1];
    }

    internal string Org { get; init; }

    internal string App { get; init; }

    internal string Value => $"app_{Org}_{App}";

    internal static bool IsResourceId(AppResourceId? resourceId)
    {
        return resourceId != null && AppIdRegex().IsMatch(resourceId.Value);
    }
}
