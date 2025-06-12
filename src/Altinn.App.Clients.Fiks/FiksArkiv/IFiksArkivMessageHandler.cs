using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Interface for composing message requests and handling received messages from FIKS Arkiv.
/// </summary>
[ImplementableByApps]
public interface IFiksArkivMessageHandler : IFiksArkivConfigValidation
{
    /// <summary>
    /// Creates a message request for the given instance.
    /// The <see cref="FiksIOMessageRequest.Payload"/> must contain a valid <see cref="FiksArkivConstants.ArchiveRecordFilename"/> document as per the NOARK-5 standard.
    /// </summary>
    /// <param name="taskId">The task which triggered the sending.</param>
    /// <param name="instance">The instance for which to compose a message request.</param>
    Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance);

    /// <summary>
    /// Handles a received message from FIKS Arkiv. The message could either be anything, for instance an acknowledgement, a receipt, or an error message.
    /// Use <see cref="FiksIOReceivedMessage.IsErrorResponse"/> to narrow it down before parsing and taking action.
    /// </summary>
    /// <param name="instance">The instance for which this message relates to.</param>
    /// <param name="receivedMessage">The received message.</param>
    /// <returns></returns>
    Task HandleReceivedMessage(Instance instance, FiksIOReceivedMessage receivedMessage);
}
