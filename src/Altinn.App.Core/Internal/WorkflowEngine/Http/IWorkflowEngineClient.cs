using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the Process Engine service.
/// </summary>
internal interface IWorkflowEngineClient
{
    /// <summary>
    /// Enqueues a process engine job via HTTP.
    /// </summary>
    /// <param name="instance">The instance</param>
    /// <param name="request">The ProcessNextRequest to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessNext(Instance instance, ProcessNextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for active jobs for a given instance ID.
    /// </summary>
    /// <param name="instance">The instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkflowStatusResponse?> GetActiveJobStatus(Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a reply for a reply command.
    /// </summary>
    Task SendReply(string correlationId, string payload, CancellationToken cancellationToken = default);
}
