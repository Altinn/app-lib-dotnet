using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the Workflow Engine service.
/// </summary>
internal interface IWorkflowEngineClient
{
    /// <summary>
    /// Enqueues one or more workflows via HTTP.
    /// </summary>
    /// <param name="request">The WorkflowEnqueueRequest to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflows(
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the details of a specific workflow by its database ID.
    /// </summary>
    /// <param name="workflowId">The workflow database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkflowStatusResponse?> GetWorkflow(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active (incomplete) workflows, optionally filtered by namespace, correlation ID, and labels.
    /// Returns an empty list when no workflows are active.
    /// </summary>
    /// <param name="ns">Namespace to filter by</param>
    /// <param name="correlationId">Optional correlation ID to filter by</param>
    /// <param name="labels">Optional label filters (key-value pairs)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        string ns,
        Guid? correlationId = null,
        Dictionary<string, string>? labels = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Requests cancellation of a workflow. Idempotent — repeated calls return the same result.
    /// </summary>
    /// <param name="workflowId">The workflow database ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CancelWorkflowResponse> CancelWorkflow(Guid workflowId, CancellationToken cancellationToken = default);
}
