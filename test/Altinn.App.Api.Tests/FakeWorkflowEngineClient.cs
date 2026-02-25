using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.WorkflowEngine;
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
    private readonly InstanceStateService _instanceStateService;

    public FakeWorkflowEngineClient(IServiceProvider serviceProvider, InstanceStateService instanceStateService)
    {
        _serviceProvider = serviceProvider;
        _instanceStateService = instanceStateService;
    }

    /// <inheritdoc />
    public async Task ProcessNext(
        Instance instance,
        ProcessNextRequest request,
        CancellationToken cancellationToken = default
    )
    {
        string org = instance.Org;
        string app = instance.AppId.Split('/')[1];
        var instanceId = new InstanceIdentifier(instance);

        // Construct the controller per call, matching MVC's transient lifetime
        var controller = new WorkflowEngineCallbackController(
            _serviceProvider,
            _serviceProvider.GetRequiredService<ILogger<WorkflowEngineCallbackController>>(),
            _serviceProvider.GetService<Telemetry>()
        );

        string currentState = request.State;

        foreach (var step in request.Steps)
        {
            if (step.Command is not Command.AppCommand appCommand)
                continue;

            // Skip Altinn event commands - they're designed to notify external systems
            // and can have issues when executed in-process (e.g., CompletedAltinnEvent
            // checks CurrentTask after UpdateProcessState has set it to null for ended processes)
            if (IsAltinnEventCommand(appCommand.CommandKey))
                continue;

            var payload = new AppCallbackPayload
            {
                CommandKey = appCommand.CommandKey,
                Actor = request.Actor,
                Payload = appCommand.Payload,
                LockToken = request.LockToken,
                State = currentState,
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
                    break;

                case BadRequestObjectResult { Value: CallbackErrorResponse error }:
                    throw new InvalidOperationException(
                        $"Callback failed for command '{appCommand.CommandKey}': {error.Message}"
                    );

                case NotFoundResult:
                    throw new InvalidOperationException(
                        $"No handler registered for command key: {appCommand.CommandKey}"
                    );

                default:
                    throw new InvalidOperationException(
                        $"Unexpected result from callback controller: {result.GetType().Name}"
                    );
            }
        }

        // Restore final state to sync the caller's instance reference.
        // This is needed because callers may inspect instance.Data / instance.Process
        // after ProcessNext returns.
        InstanceDataUnitOfWork finalState = await _instanceStateService.RestoreState(
            currentState,
            request.Actor.Language
        );
        instance.Data.Clear();
        instance.Data.AddRange(finalState.Instance.Data);
        instance.Process = finalState.Instance.Process;
    }

    /// <inheritdoc />
    public Task<WorkflowStatusResponse?> GetActiveJobStatus(
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        // In synchronous test mode, jobs complete immediately, so there's never an active job
        return Task.FromResult<WorkflowStatusResponse?>(null);
    }

    /// <summary>
    /// Checks if a command is an Altinn event notification command.
    /// These commands are designed to notify external systems and can have issues
    /// when executed in-process due to timing assumptions about instance state.
    /// </summary>
    private static bool IsAltinnEventCommand(string commandKey) =>
        commandKey.EndsWith("AltinnEvent", StringComparison.OrdinalIgnoreCase);
}
