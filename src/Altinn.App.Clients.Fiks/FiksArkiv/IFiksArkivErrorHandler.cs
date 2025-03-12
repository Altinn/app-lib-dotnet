using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

// TODO: This is not ready for implementation yet, as we have no correlation between the instance and the received message.
internal interface IFiksArkivErrorHandler
{
    Task HandleError(Instance instance, FiksIOReceivedMessageArgs receivedMessage);
}
