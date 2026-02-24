using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Api.Tests;

/// <summary>
/// In-process implementation of <see cref="IWorkflowEngineClient"/> for testing.
/// Executes process engine commands synchronously without HTTP calls.
/// </summary>
internal sealed class FakeWorkflowEngineClient : IWorkflowEngineClient
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInstanceClient _instanceClient;
    private readonly InstanceStateService _instanceStateService;

    public FakeWorkflowEngineClient(
        IServiceProvider serviceProvider,
        IInstanceClient instanceClient,
        InstanceStateService instanceStateService
    )
    {
        _serviceProvider = serviceProvider;
        _instanceClient = instanceClient;
        _instanceStateService = instanceStateService;
    }

    /// <inheritdoc />
    public async Task ProcessNext(
        Instance instance,
        ProcessNextRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var appId = new AppIdentifier(instance.Org, instance.AppId.Split('/')[1]);
        var instanceId = new InstanceIdentifier(instance);

        // Generate a lock token for this in-process session to enable caching
        // across multiple commands. This simulates the distributed lock behavior
        // of the real process engine.
        string lockToken = Guid.NewGuid().ToString();

        // Restore state from the opaque blob, mirroring how the real engine
        // echoes state back to the callback controller.
        InstanceDataUnitOfWork instanceDataUnitOfWork = await _instanceStateService.RestoreState(
            request.State,
            request.Actor.Language
        );

        // Execute all commands with the same unit of work
        foreach (var taskRequest in request.Steps)
        {
            if (taskRequest.Command is Command.AppCommand appCommand)
            {
                // Skip Altinn event commands - they're designed to notify external systems
                // and can have issues when executed in-process (e.g., CompletedAltinnEvent
                // checks CurrentTask after UpdateProcessState has set it to null for ended processes)
                if (IsAltinnEventCommand(appCommand.CommandKey))
                {
                    continue;
                }

                await ExecuteAppCommand(
                    appId,
                    instanceId,
                    appCommand,
                    request.Actor,
                    lockToken,
                    instanceDataUnitOfWork,
                    cancellationToken
                );
            }
            // Skip non-app commands (Timeout, Webhook, etc.) as they're not relevant in test mode
        }

        // Save all changes at the end
        // UpdateInstanceData creates new data elements and deletes removed ones
        DataElementChanges changes = instanceDataUnitOfWork.GetDataElementChanges(false);
        await instanceDataUnitOfWork.UpdateInstanceData(changes);
        // SaveChanges handles updates to existing data elements
        await instanceDataUnitOfWork.SaveChanges(changes);

        // Persist the process state to storage (UpdateProcessState command only updates in-memory)
        await _instanceClient.UpdateProcess(
            instanceDataUnitOfWork.Instance,
            StorageAuthenticationMethod.ServiceOwner(),
            cancellationToken
        );

        // Update the original instance's Data property so callers see the created data elements
        // This mimics what would happen if they re-fetched the instance from storage
        instance.Data.Clear();
        instance.Data.AddRange(instanceDataUnitOfWork.Instance.Data);

        // Also update the Process state so callers see the new state
        instance.Process = instanceDataUnitOfWork.Instance.Process;
    }

    private async Task ExecuteAppCommand(
        AppIdentifier appId,
        InstanceIdentifier instanceId,
        Command.AppCommand appCommand,
        Actor actor,
        string lockToken,
        InstanceDataUnitOfWork instanceDataUnitOfWork,
        CancellationToken cancellationToken
    )
    {
        string commandKey = appCommand.CommandKey;

        IWorkflowEngineCommand? command = _serviceProvider
            .GetServices<IWorkflowEngineCommand>()
            .FirstOrDefault(x => x.GetKey() == commandKey);

        if (command is null)
        {
            throw new InvalidOperationException($"No handler registered for command key: {commandKey}");
        }

        var payload = new AppCallbackPayload()
        {
            CommandKey = commandKey,
            Actor = actor,
            Payload = appCommand.Payload,
            LockToken = lockToken,
            State = default,
        };

        ProcessEngineCommandResult result = await command.Execute(
            new ProcessEngineCommandContext
            {
                AppId = appId,
                InstanceId = instanceId,
                InstanceDataMutator = instanceDataUnitOfWork,
                CancellationToken = cancellationToken,
                Payload = payload,
            }
        );

        switch (result)
        {
            case SuccessfulProcessEngineCommandResult:
                // Don't save after each command - we'll save once at the end
                break;

            case FailedProcessEngineCommandResult failed:
                throw new InvalidOperationException(
                    $"Process engine command '{commandKey}' failed: {failed.ErrorMessage}"
                );

            default:
                throw new InvalidOperationException($"Unexpected result type: {result.GetType().Name}");
        }
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
