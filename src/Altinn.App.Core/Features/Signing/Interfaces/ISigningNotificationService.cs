using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for implementing app-specific logic for sending notifications about apps for signing.
/// </summary>
public interface ISigningNotificationService
{
    /// <summary>
    /// Sends notifications to signees about the signing task.
    /// </summary>
    public Task<List<SigneeContext>> NotifySignatureTask(
        List<SigneeContext> signeeContexts,
        CancellationToken? ct = null
    );
}
