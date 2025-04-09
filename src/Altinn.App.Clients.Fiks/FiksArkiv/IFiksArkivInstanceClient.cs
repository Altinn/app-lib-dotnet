using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal interface IFiksArkivInstanceClient
{
    Task<string> GetServiceOwnerAccessToken();
    Task<Instance> GetInstance(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
    Task ProcessMoveNext(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
    Task MarkInstanceComplete(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
    Task<DataElement> InsertBinaryData(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        string dataType,
        string contentType,
        string filename,
        Stream stream,
        string? generatedFromTask = null
    );
}
