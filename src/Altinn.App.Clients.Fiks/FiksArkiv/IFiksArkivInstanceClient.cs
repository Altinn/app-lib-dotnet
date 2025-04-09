using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal interface IFiksArkivInstanceClient
{
    Task<string> GetServiceOwnerAccessToken();
    Task<Instance> GetInstance(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
    Task ProcessMoveNext(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
    Task MarkInstanceComplete(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
}
