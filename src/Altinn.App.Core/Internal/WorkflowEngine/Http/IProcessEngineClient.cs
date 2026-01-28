using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the Process Engine service.
/// </summary>
internal interface IProcessEngineClient
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
    Task<ProcessEngineStatusResponse?> GetActiveJobStatus(
        Instance instance,
        CancellationToken cancellationToken = default
    );
}
