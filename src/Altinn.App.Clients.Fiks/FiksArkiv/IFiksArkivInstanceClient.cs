using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal interface IFiksArkivInstanceClient
{
    Task<Instance> GetInstance(InstanceIdentifier instanceIdentifier);
    Task ProcessMoveNext(InstanceIdentifier instanceIdentifier, string? action = null);
    Task MarkInstanceComplete(InstanceIdentifier instanceIdentifier);
    Task<DataElement> InsertBinaryData(
        InstanceIdentifier instanceIdentifier,
        string dataType,
        string contentType,
        string filename,
        Stream stream,
        string? generatedFromTask = null
    );
}
