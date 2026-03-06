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
        Guid workflowId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lists all active (incomplete) workflows for the given instance.
    /// Returns an empty list when no workflows are active.
    /// </summary>
    /// <param name="instance">The instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        Instance instance,
        CancellationToken cancellationToken = default
    );
}
