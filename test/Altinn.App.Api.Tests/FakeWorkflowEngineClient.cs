using System.Text.Json;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;
using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;
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
    private readonly InstanceStateService _instanceStateService;

    public FakeWorkflowEngineClient(IServiceProvider serviceProvider, InstanceStateService instanceStateService)
    {
        _serviceProvider = serviceProvider;
        _instanceStateService = instanceStateService;
    }

    /// <inheritdoc />
    public async Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflows(
        string ns,
        string idempotencyKey,
        Guid? correlationId,
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Extract context from request
        var context = request.Context is { } ctx
            ? JsonSerializer.Deserialize<AppWorkflowContext>(ctx)
                ?? throw new InvalidOperationException("Failed to deserialize AppWorkflowContext from request")
            : throw new InvalidOperationException("WorkflowEnqueueRequest.Context is required");

        string org = context.Org;
        string app = context.App;
        int instanceOwnerPartyId = context.InstanceOwnerPartyId;
        Guid instanceGuid = context.InstanceGuid;

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
                if (step.Command.Type != "app" || step.Command.Data is not { } data)
                    continue;

                var appCommandData =
                    JsonSerializer.Deserialize<AppCommandData>(data)
                    ?? throw new InvalidOperationException("Failed to deserialize AppCommandData");

                // Skip Altinn event commands - they're designed to notify external systems
                // and can have issues when executed in-process (e.g., CompletedAltinnEvent
                // checks CurrentTask after SaveProcessStateToStorage has set it to null for ended processes)
                if (IsAltinnEventCommand(appCommandData.CommandKey))
                    continue;

                var payload = new AppCallbackPayload
                {
                    CommandKey = appCommandData.CommandKey,
                    Actor = context.Actor,
                    Payload = appCommandData.Payload,
                    LockToken = context.LockToken,
                    State = currentState,
                    WorkflowId = databaseId,
                };

                IActionResult result = await controller.ExecuteCommand(
                    org,
                    app,
                    instanceOwnerPartyId,
                    instanceGuid,
                    appCommandData.CommandKey,
                    payload,
                    cancellationToken
                );

                switch (result)
                {
                    case OkObjectResult { Value: AppCallbackResponse response }:
                        currentState = response.State;

                        if (appCommandData.CommandKey == SaveProcessStateToStorage.Key)
                        {
                            processStateCommitted = true;
                        }
                        break;

                    case ObjectResult { Value: ProblemDetails problem }:
                        // Post-commit failures (e.g., ExecuteServiceTask) should not abort the workflow.
                        // The process state has already been saved to Storage. Record the failure.
                        if (processStateCommitted && appCommandData.CommandKey == ExecuteServiceTask.Key)
                        {
                            string serviceTaskType = "unknown";
                            if (appCommandData.Payload is not null)
                            {
                                var stPayload = CommandPayloadSerializer.Deserialize<ExecuteServiceTaskPayload>(
                                    appCommandData.Payload
                                );
                                if (stPayload is not null)
                                    serviceTaskType = stPayload.ServiceTaskType;
                            }
                            serviceTaskFailure = new ServiceTaskFailedException(serviceTaskType, problem.Detail);
                            goto workflowDone;
                        }

                        throw new InvalidOperationException(
                            $"Callback failed for command '{appCommandData.CommandKey}': {problem.Title}: {problem.Detail}"
                        );

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected result from callback controller: {result.GetType().Name}"
                        );
                }
            }

            workflowDone:

            // Throw after processing - the caller (ProcessEngine.WaitForWorkflowsAndRefetchInstance)
            // will refetch from Storage
            if (serviceTaskFailure is not null)
            {
                throw serviceTaskFailure;
            }

            workflowResults.Add(
                new WorkflowResult
                {
                    Ref = workflow.Ref,
                    DatabaseId = databaseId,
                    Namespace = ns,
                }
            );
        }

        return new WorkflowEnqueueResponse.Accepted { Workflows = workflowResults };
    }

    /// <inheritdoc />
    public Task<WorkflowStatusResponse?> GetWorkflow(
        string ns,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, jobs complete immediately, so there's never an active job
        return Task.FromResult<WorkflowStatusResponse?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        string ns,
        Guid? correlationId = null,
        Dictionary<string, string>? labels = null,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, all workflows complete immediately during EnqueueWorkflows
        return Task.FromResult<IReadOnlyList<WorkflowStatusResponse>>([]);
    }

    /// <inheritdoc />
    public Task<CancelWorkflowResponse> CancelWorkflow(
        string ns,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, cancellation is a no-op
        return Task.FromResult(new CancelWorkflowResponse(workflowId, DateTimeOffset.UtcNow, true));
    }

    /// <inheritdoc />
    public Task<ResumeWorkflowResponse> ResumeWorkflow(
        string ns,
        Guid workflowId,
        bool cascade = false,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(new ResumeWorkflowResponse(workflowId, DateTimeOffset.UtcNow, []));
    }

    /// <summary>
    /// Checks if a command is an Altinn event notification command.
    /// These commands are designed to notify external systems and can have issues
    /// when executed in-process due to timing assumptions about instance state.
    /// </summary>
    private static bool IsAltinnEventCommand(string commandKey) =>
        commandKey.EndsWith("AltinnEvent", StringComparison.OrdinalIgnoreCase);
}
