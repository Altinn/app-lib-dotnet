using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Contains logic for interacting with the correpondence message service
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    /// Provides access to known authorisation methods
    /// </summary>
    ICorrespondenceAuthorisationFactory Authorisation { get; }

    /// <summary>
    /// Sends a correspondence
    /// </summary>
    /// <param name="payload">The <see cref="CorrespondencePayload.Send"/> payload</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns></returns>
    Task<CorrespondenceResponse.Send> Send(
        CorrespondencePayload.Send payload,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches the status of a correspondence
    /// </summary>
    /// <param name="payload">The <see cref="CorrespondencePayload.Status"/> payload</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    /// <returns></returns>
    Task<CorrespondenceResponse.Status> Status(
        CorrespondencePayload.Status payload,
        CancellationToken cancellationToken = default
    );
}
