using Altinn.App.Core.Features.Correspondence.Builder;

namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Contains logic for interacting with the correpondence message service
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    /// Sends a correspondence message
    /// </summary>
    /// <param name="content">The content to send</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    Task Send(Models.Correspondence content, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a correspondence message
    /// </summary>
    /// <param name="builder">The builder content to send</param>
    /// <param name="cancellationToken">An optional cancellation token</param>
    Task Send(CorrespondenceBuilder builder, CancellationToken cancellationToken);
}
