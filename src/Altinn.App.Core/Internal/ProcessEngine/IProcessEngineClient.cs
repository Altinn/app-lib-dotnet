using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// HTTP client for communicating with the Process Engine service.
/// </summary>
internal interface IProcessEngineClient
{
    /// <summary>
    /// Enqueues a process engine job via HTTP.
    /// </summary>
    /// <param name="request">The ProcessNextRequest to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessNext(ProcessNextRequest request, CancellationToken cancellationToken = default);
}
