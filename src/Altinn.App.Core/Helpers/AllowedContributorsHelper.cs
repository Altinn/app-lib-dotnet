using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers;

internal static class AllowedContributorsHelper
{
    public static bool IsValidContributor(DataType dataType, Authenticated auth)
    {
        if (dataType.AllowedContributers is null || dataType.AllowedContributers.Count == 0)
        {
            return true;
        }

        var (org, orgNr) = auth switch
        {
            Authenticated.Org a => (null, a.OrgNo),
            Authenticated.ServiceOwner a => (a.Name, a.OrgNo),
            Authenticated.SystemUser a => (null, a.SystemUserOrgNr.Get(OrganisationNumberFormat.Local)),
            _ => (null, null),
        };

        foreach (string item in dataType.AllowedContributers)
        {
            var splitIndex = item.IndexOf(':');
            ReadOnlySpan<char> key = item.AsSpan(0, splitIndex);
            ReadOnlySpan<char> value = item.AsSpan(splitIndex + 1);

            if (key.Equals("org", StringComparison.OrdinalIgnoreCase))
            {
                if (org is null)
                    continue;

                if (value.Equals(org, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (key.Equals("orgno", StringComparison.OrdinalIgnoreCase))
            {
                if (orgNr is null)
                    continue;

                if (value.Equals(orgNr, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void EnsureDataTypeIsAppOwned(ApplicationMetadata metadata, string? dataTypeId)
    {
        if (dataTypeId is null)
        {
            return;
        }

        DataType? dataType = metadata.DataTypes.Find(x => x.Id == dataTypeId);
        List<string>? allowedContributors = dataType?.AllowedContributers;

        if (
            allowedContributors is null
            || !(allowedContributors.Count == 1 && allowedContributors.Contains("app:owned"))
        )
        {
            throw new ApplicationConfigException(
                $"AllowedContributors must be set to ['app:owned'] on the data type ${dataType?.Id}. This is to prevent editing of the data type through the API."
            );
        }
    }
}
