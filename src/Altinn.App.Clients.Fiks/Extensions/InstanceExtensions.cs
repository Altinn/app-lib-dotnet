using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class InstanceExtensions
{
    public static IEnumerable<DataElement> GetOptionalDataElements(this Instance instance, string dataType)
    {
        return instance.Data.Where(x => x.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
    }

    public static DataElement GetRequiredDataElement(this Instance instance, string dataType)
    {
        return instance.Data.FirstOrDefault(x => x.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase))
            ?? throw new FiksArkivException($"Fiks Arkiv error: No data elements found for DataType '{dataType}'");
    }

    public static string GetInstanceUrl(this Instance instance, GeneralSettings generalSettings)
    {
        var appIdentifier = new AppIdentifier(instance);
        var instanceIdentifier = new InstanceIdentifier(instance);
        var baseUrl = generalSettings.FormattedExternalAppBaseUrl(appIdentifier);

        return $"{baseUrl}instances/{instanceIdentifier}";
    }
}
