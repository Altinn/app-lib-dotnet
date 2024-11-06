using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Contains logic for interacting with the correpondence message service
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    /// Sends a correspondence message.
    /// </summary>
    /// <param name="content">The <see cref="CorrespondenceRequest"/> payload to send.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns></returns>
    Task<CorrespondenceResponse> Send(CorrespondenceRequest content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a correspondence message.
    /// </summary>
    /// <param name="builder">The correspondence builder to construct a <see cref="CorrespondenceRequest"/> payload from</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns></returns>
    Task<CorrespondenceResponse> Send(
        ICorrespondenceBuilderCanBuild builder,
        CancellationToken cancellationToken = default
    );
}
