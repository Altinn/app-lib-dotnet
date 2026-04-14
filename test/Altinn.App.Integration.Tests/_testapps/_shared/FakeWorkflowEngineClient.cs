using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;
using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#nullable enable

namespace TestApp.Shared;

/// <summary>
/// In-process implementation of <see cref="IWorkflowEngineClient"/> for integration testing.
/// Simulates the workflow engine by calling <see cref="WorkflowEngineCallbackController"/>
/// directly per command, exercising the real controller code path.
/// </summary>
internal sealed class FakeWorkflowEngineClient : IWorkflowEngineClient
{
    private readonly IServiceProvider _serviceProvider;

    public FakeWorkflowEngineClient(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflows(
        string ns,
        string idempotencyKey,
        Guid? correlationId,
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    )
    {
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

            var controller = new WorkflowEngineCallbackController(
                _serviceProvider,
                _serviceProvider.GetRequiredService<ILogger<WorkflowEngineCallbackController>>(),
                _serviceProvider.GetService<Telemetry>()
            );

            string? currentState = workflow.State;

            foreach (var step in workflow.Steps)
            {
                if (step.Command.Type != "app" || step.Command.Data is not { } data)
                    continue;

                var appCommandData =
                    JsonSerializer.Deserialize<AppCommandData>(data)
                    ?? throw new InvalidOperationException("Failed to deserialize AppCommandData");

                // Skip Altinn event commands - they notify external systems and can have
                // timing issues when executed in-process
                if (appCommandData.CommandKey.EndsWith("AltinnEvent", StringComparison.OrdinalIgnoreCase))
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
                        break;

                    case ObjectResult { Value: ProblemDetails problem }:
                        throw new InvalidOperationException(
                            $"Callback failed for command '{appCommandData.CommandKey}': {problem.Title}: {problem.Detail}"
                        );

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected result from callback controller: {result.GetType().Name}"
                        );
                }
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

    public Task<WorkflowStatusResponse?> GetWorkflow(
        string ns,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<WorkflowStatusResponse?>(null);
    }

    public Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        string ns,
        Guid? correlationId = null,
        Dictionary<string, string>? labels = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<IReadOnlyList<WorkflowStatusResponse>>([]);
    }

    public Task<CancelWorkflowResponse> CancelWorkflow(
        string ns,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(new CancelWorkflowResponse(workflowId, DateTimeOffset.UtcNow, true));
    }

    public Task<ResumeWorkflowResponse> ResumeWorkflow(
        string ns,
        Guid workflowId,
        bool cascade = false,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(new ResumeWorkflowResponse(workflowId, DateTimeOffset.UtcNow, []));
    }
}
