using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivMessageProvider
{
    Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance);
}
