namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Contains logic for interacting with the correpondence message service
/// </summary>
public interface ICorrespondenceClient
{
    /// <summary>
    /// Sends a correspondence message.
    /// </summary>
    /// <param name="content">The message to send.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns></returns>
    Task Send(Models.Correspondence content, CancellationToken cancellationToken);
}
