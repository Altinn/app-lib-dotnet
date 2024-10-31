using Altinn.App.Core.Configuration;

namespace Altinn.App.Core.Internal.AccessManagement.Helpers;

internal sealed class UrlHelper(PlatformSettings platformSettings)
{
    internal string CreateInstanceDelegationUrl(string appResourceId, string instanceId)
    {
        return platformSettings.ApiAccessManagementEndpoint.TrimEnd('/') + "/app/delegations/resource/" + appResourceId + "/instance/" + instanceId;
    }
}
