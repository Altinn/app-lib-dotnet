using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Interface for determining whether a FIKS Arkiv message should be sent.
/// </summary>
[ImplementableByApps]
public interface IFiksArkivAutoSendDecision
{
    /// <summary>
    /// Determines whether a FIKS Arkiv message should be sent or not.
    /// </summary>
    /// <param name="taskId">The task that has just finished.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<bool> ShouldSend(string taskId, Instance instance, CancellationToken cancellationToken = default);
}
