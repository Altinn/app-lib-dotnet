using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Api.Tests;

/// <summary>
/// In-process implementation of <see cref="IWorkflowEngineClient"/> for testing.
/// Simulates the workflow engine by calling <see cref="WorkflowEngineCallbackController"/>
/// directly per command, exercising the real controller code path including
/// per-command state round-trips, data saves, and abandon checks.
/// </summary>
internal sealed class FakeWorkflowEngineClient : IWorkflowEngineClient
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkflowStateSnapshotService _snapshotService;

    public FakeWorkflowEngineClient(IServiceProvider serviceProvider, WorkflowStateSnapshotService snapshotService)
    {
        _serviceProvider = serviceProvider;
        _snapshotService = snapshotService;
    }

    /// <inheritdoc />
    public async Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflow(
        Instance instance,
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        string org = instance.Org;
        string app = instance.AppId.Split('/')[1];
        var instanceId = new InstanceIdentifier(instance);

        var workflowResults = new List<WorkflowResult>();

        foreach (var workflow in request.Workflows)
        {
            Guid databaseId = Guid.NewGuid();

            // Construct the controller per call, matching MVC's transient lifetime
            var controller = new WorkflowEngineCallbackController(
                _serviceProvider,
                _serviceProvider.GetRequiredService<ILogger<WorkflowEngineCallbackController>>(),
                _serviceProvider.GetService<Telemetry>()
            );

            string? currentState = workflow.State;
            bool processStateCommitted = false;
            ServiceTaskFailedException? serviceTaskFailure = null;

            foreach (var step in workflow.Steps)
            {
                if (step.Command is not Command.AppCommand appCommand)
                    continue;

                // Skip Altinn event commands - they're designed to notify external systems
                // and can have issues when executed in-process (e.g., CompletedAltinnEvent
                // checks CurrentTask after SaveProcessStateToStorage has set it to null for ended processes)
                if (IsAltinnEventCommand(appCommand.CommandKey))
                    continue;

                var payload = new AppCallbackPayload
                {
                    CommandKey = appCommand.CommandKey,
                    Actor = request.Actor,
                    Payload = appCommand.Payload,
                    LockToken = request.LockToken ?? string.Empty,
                    State = currentState,
                    WorkflowId = databaseId,
                };

                IActionResult result = await controller.ExecuteCommand(
                    org,
                    app,
                    instanceId.InstanceOwnerPartyId,
                    instanceId.InstanceGuid,
                    appCommand.CommandKey,
                    payload,
                    cancellationToken
                );

                switch (result)
                {
                    case OkObjectResult { Value: AppCallbackResponse response }:
                        currentState = response.State;

                        if (appCommand.CommandKey == SaveProcessStateToStorage.Key)
                        {
                            processStateCommitted = true;
                        }
                        break;

                    case ObjectResult { Value: ProblemDetails problem }:
                        // Post-commit failures (e.g., ExecuteServiceTask) should not abort the workflow.
                        // The process state has already been saved to Storage. Sync state and record the failure.
                        if (processStateCommitted && appCommand.CommandKey == ExecuteServiceTask.Key)
                        {
                            string serviceTaskType = instance.Process?.CurrentTask?.AltinnTaskType ?? "unknown";
                            serviceTaskFailure = new ServiceTaskFailedException(serviceTaskType, problem.Detail);
                            goto workflowDone;
                        }

                        throw new InvalidOperationException(
                            $"Callback failed for command '{appCommand.CommandKey}': {problem.Title}: {problem.Detail}"
                        );

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected result from callback controller: {result.GetType().Name}"
                        );
                }
            }

            workflowDone:

            // Restore final state to sync the caller's instance reference.
            // This is needed because callers may inspect instance.Data / instance.Process
            // after EnqueueWorkflow returns.
            if (currentState is not null)
            {
                WorkflowStateSnapshot snapshot = WorkflowStateSnapshotService.Deserialize(currentState);
                InstanceDataUnitOfWork finalState = await _snapshotService.RestoreSnapshot(
                    snapshot,
                    request.Actor.Language
                );
                instance.Data.Clear();
                instance.Data.AddRange(finalState.Instance.Data);
                instance.Process = finalState.Instance.Process;
            }

            // Throw after syncing state, so the caller's instance reference has the committed process state
            if (serviceTaskFailure is not null)
            {
                throw serviceTaskFailure;
            }

            workflowResults.Add(new WorkflowResult { Ref = workflow.Ref, DatabaseId = databaseId });
        }

        return new WorkflowEnqueueResponse.Accepted { Workflows = workflowResults };
    }

    /// <inheritdoc />
    public Task<WorkflowStatusResponse?> GetWorkflowStatus(
        Instance instance,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, jobs complete immediately, so there's never an active job
        return Task.FromResult<WorkflowStatusResponse?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, all workflows complete immediately during EnqueueWorkflow
        return Task.FromResult<IReadOnlyList<WorkflowStatusResponse>>([]);
    }

    /// <summary>
    /// Checks if a command is an Altinn event notification command.
    /// These commands are designed to notify external systems and can have issues
    /// when executed in-process due to timing assumptions about instance state.
    /// </summary>
    private static bool IsAltinnEventCommand(string commandKey) =>
        commandKey.EndsWith("AltinnEvent", StringComparison.OrdinalIgnoreCase);
}
