using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivInstanceClient
{
    Task<string> GetServiceOwnerAccessToken();
    Task<Instance> GetInstance(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier);
}
