using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

[ImplementableByApps]
public interface IFiksArkivMessageProvider : IFiksArkivConfigValidation
{
    Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance);
}
