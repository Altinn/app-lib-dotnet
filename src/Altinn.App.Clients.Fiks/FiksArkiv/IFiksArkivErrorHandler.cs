using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivErrorHandler
{
    Task HandleError(Instance instance, FiksIOReceivedMessageArgs receivedMessage);
}
