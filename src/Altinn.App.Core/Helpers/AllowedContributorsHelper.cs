using System.Globalization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers;

internal static class AllowedContributorsHelper
{
    public static bool IsValidContributor(DataType dataType, string? org, int? orgNr)
    {
        if (dataType.AllowedContributers is null || dataType.AllowedContributers.Count == 0)
        {
            return true;
        }

        foreach (string item in dataType.AllowedContributers)
        {
            string key = item.Split(':')[0];
            string value = item.Split(':')[1];

            switch (key.ToLowerInvariant())
            {
                case "app":
                    return false;

                case "org":
                    if (value.Equals(org, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    break;
                case "orgno":
                    if (value.Equals(orgNr?.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    public static void EnsureDataTypeIsAppOwned(ApplicationMetadata metadata, string? dataTypeId)
    {
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
