using Altinn.App.Clients.Fiks.Exceptions;
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
}
