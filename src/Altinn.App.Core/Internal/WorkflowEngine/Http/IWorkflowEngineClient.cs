using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the Workflow Engine service.
/// </summary>
internal interface IWorkflowEngineClient
{
    /// <summary>
    /// Enqueues a workflow via HTTP.
    /// </summary>
    /// <param name="instance">The instance</param>
    /// <param name="request">The WorkflowEnqueueRequest to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflow(
        Instance instance,
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the status of a specific workflow by its database ID.
    /// </summary>
    /// <param name="instance">The instance</param>
    /// <param name="workflowId">The workflow database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkflowStatusResponse?> GetWorkflowStatus(
        Instance instance,
        long workflowId,
        CancellationToken cancellationToken = default
    );
}
