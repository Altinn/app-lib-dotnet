using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

// TODO: This is not ready for implementation yet, as we have no correlation between the instance and the received message.
[ImplementableByApps]
internal interface IFiksArkivErrorHandler : IFiksArkivConfigValidation
{
    Task HandleError(Instance instance, FiksIOReceivedMessageArgs receivedMessage);
}
