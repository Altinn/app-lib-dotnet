using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Orchestrator of the sending and receiving of messages via Fiks Arkiv.
/// </summary>
internal interface IFiksArkivHost : IFiksArkivConfigValidation
{
    /// <summary>
    /// Generates a message of the given type for the given instance and sends it via Fiks Arkiv.
    /// The content of the message is generated using the configured <see cref="IFiksArkivPayloadGenerator"/>,
    /// which must be capable of generating the given message type.
    /// </summary>
    /// <param name="taskId">The task ID the message is generated from</param>
    /// <param name="instance">The instance the message relates to</param>
    /// <param name="messageType">The Fiks Arkiv message type (create, update, etc)</param>
    /// <returns></returns>
    Task<FiksIOMessageResponse> GenerateAndSendMessage(string taskId, Instance instance, string messageType);
}
