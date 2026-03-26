using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.AltinnEvents;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.ProcessEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskAbandon;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskStart;
using Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;
using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;
using Altinn.App.Core.Models.Notifications.Future;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Defines a group of commands that should be executed for a process event.
/// </summary>
internal sealed class WorkflowCommandSet
{
    private readonly List<StepRequest> _commands = [];
    private readonly List<StepRequest> _postProcessNextCommittedCommands = [];

    /// <summary>
    /// Gets the main commands for this event. SaveProcessStateToStorage will be added after these.
    /// </summary>
    public IReadOnlyList<StepRequest> Commands => _commands;

    /// <summary>
    /// Gets the commands that execute after the ProcessNext has been committed to storage (e.g., MovedToAltinnEvent).
    /// </summary>
    public IReadOnlyList<StepRequest> PostProcessNextCommittedCommands => _postProcessNextCommittedCommands;

    /// <summary>
    /// Creates command group for task start events.
    /// </summary>
    /// <param name="serviceTaskType">If this is a service task, the task type identifier. Otherwise null.</param>
    /// <param name="isInitialTaskStart">True if this is the first task start (process is starting), false for subsequent task transitions.</param>
    /// <param name="prefill">Prefill data for initial task start. Only relevant when isInitialTaskStart is true.</param>
    /// <param name="notification">Notification to send to instance owner on instantiation. Only relevant when isInitialTaskStart is true.</param>
    /// <param name="registerEvents">Whether to register events with the events component. Controlled by AppSettings.RegisterEventsWithEventsComponent.</param>
    public static WorkflowCommandSet GetTaskStartSteps(
        string? serviceTaskType,
        bool isInitialTaskStart,
        Dictionary<string, string>? prefill = null,
        InstantiationNotification? notification = null,
        bool registerEvents = false
    )
    {
        var group = new WorkflowCommandSet()
            .AddCommand(UnlockTaskData.Key)
            .AddCommand(StartTaskLegacyHook.Key, new StartTaskLegacyHookPayload(prefill))
            .AddCommand(OnTaskStartingHook.Key)
            .AddCommand(CommonTaskInitialization.Key, new CommonTaskInitializationPayload(prefill))
            .AddCommand(StartTask.Key);

        if (registerEvents)
        {
            group.AddPostProcessNextCommittedCommand(MovedToAltinnEvent.Key);
        }

        if (serviceTaskType is not null)
        {
            group.AddPostProcessNextCommittedCommand(
                ExecuteServiceTask.Key,
                new ExecuteServiceTaskPayload(serviceTaskType)
            );
        }

        if (isInitialTaskStart && registerEvents)
        {
            group.AddPostProcessNextCommittedCommand(InstanceCreatedAltinnEvent.Key);

            if (notification is not null)
            {
                group.AddPostProcessNextCommittedCommand(
                    NotifyInstanceOwnerOnInstantiation.Key,
                    new NotifyInstanceOwnerOnInstantiationPayload(notification)
                );
            }
        }

        return group;
    }

    /// <summary>
    /// Creates command group for task end events.
    /// </summary>
    public static WorkflowCommandSet GetTaskEndSteps()
    {
        return new WorkflowCommandSet()
            .AddCommand(EndTask.Key)
            .AddCommand(CommonTaskFinalization.Key)
            .AddCommand(EndTaskLegacyHook.Key)
            .AddCommand(OnTaskEndingHook.Key)
            .AddCommand(LockTaskData.Key);
    }

    /// <summary>
    /// Creates command group for task abandon events.
    /// </summary>
    public static WorkflowCommandSet GetTaskAbandonSteps()
    {
        return new WorkflowCommandSet()
            .AddCommand(AbandonTask.Key)
            .AddCommand(OnTaskAbandonHook.Key)
            .AddCommand(AbandonTaskLegacyHook.Key);
    }

    /// <summary>
    /// Creates command group for process end events.
    /// </summary>
    /// <param name="registerEvents">Whether to register events with the events component. Controlled by AppSettings.RegisterEventsWithEventsComponent.</param>
    /// <param name="hasAutoDeleteDataTypes">Whether any data types have AutoDeleteOnProcessEnd enabled.</param>
    /// <param name="autoDeleteInstanceOnProcessEnd">Whether the application is configured to auto-delete the instance on process end.</param>
    public static WorkflowCommandSet GetProcessEndSteps(
        bool registerEvents = false,
        bool hasAutoDeleteDataTypes = false,
        bool autoDeleteInstanceOnProcessEnd = false
    )
    {
        // EndProcessLegacyHook runs post-commit because IProcessEnd.End reads instance.Process.EndEvent,
        // which is only set when the process state is persisted. This matches the old ProcessEngine behavior
        // where RunAppDefinedProcessEndHandlers ran after HandleEventsAndUpdateStorage.
        var group = new WorkflowCommandSet()
            .AddCommand(OnProcessEndingHook.Key)
            .AddPostProcessNextCommittedCommand(EndProcessLegacyHook.Key);

        if (hasAutoDeleteDataTypes)
        {
            group.AddPostProcessNextCommittedCommand(DeleteDataElementsIfConfigured.Key);
        }

        if (autoDeleteInstanceOnProcessEnd)
        {
            group.AddPostProcessNextCommittedCommand(DeleteInstanceIfConfigured.Key);
        }

        if (registerEvents)
        {
            group.AddPostProcessNextCommittedCommand(CompletedAltinnEvent.Key);
        }

        return group;
    }

    /// <summary>
    /// Adds a command to the main sequence.
    /// </summary>
    private WorkflowCommandSet AddCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        _commands.Add(CreateCommand(commandKey, payload));
        return this;
    }

    /// <summary>
    /// Adds a command that executes after the ProcessNext has been committed to storage via SaveProcessStateToStorage.
    /// </summary>
    private WorkflowCommandSet AddPostProcessNextCommittedCommand(
        string commandKey,
        CommandRequestPayload? payload = null
    )
    {
        _postProcessNextCommittedCommands.Add(CreateCommand(commandKey, payload));
        return this;
    }

    private static StepRequest CreateCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new StepRequest
        {
            OperationId = commandKey,
            Command = CommandDefinition.Create(
                "app",
                new AppCommandData { CommandKey = commandKey, Payload = serializedPayload }
            ),
        };
    }
}
