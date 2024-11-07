using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Contains logic for interacting with the correpondence message service
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    /// Sends a correspondence
    /// </summary>
    /// <param name="payload">The <see cref="SendCorrespondencePayload"/> payload to send</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns></returns>
    Task<CorrespondenceResponse> Send(SendCorrespondencePayload payload, CancellationToken cancellationToken = default);
}
