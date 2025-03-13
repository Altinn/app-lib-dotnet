using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class InstanceExtensions
{
    public static IEnumerable<DataElement> GetRequiredDataElements(this Instance instance, string dataType)
    {
        var dataElements = instance.Data.Where(x => x.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));

        return dataElements.Any()
            ? dataElements
            : throw new Exception($"Fiks Arkiv error: No data elements found for DataType '{dataType}'");
    }

    public static IEnumerable<DataElement> GetOptionalDataElements(this Instance instance, string dataType)
    {
        return instance.Data.Where(x => x.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
    }

    public static DataElement GetRequiredDataElement(this Instance instance, string dataType)
    {
        return instance.GetRequiredDataElements(dataType).First();
    }
}
