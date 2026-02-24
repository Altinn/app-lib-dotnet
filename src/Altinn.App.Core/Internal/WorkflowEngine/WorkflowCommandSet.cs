using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.AltinnEvents;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.ProcessEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskAbandon;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskStart;
using Altinn.App.Core.Internal.WorkflowEngine.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Defines a group of commands that should be executed for a process event.
/// </summary>
internal sealed class WorkflowCommandSet
{
    private readonly List<StepRequest> _commands = [];
    private readonly List<StepRequest> _postProcessNextCommittedCommands = [];

    /// <summary>
    /// Gets the main commands for this event. UpdateProcessState will be added after these.
    /// </summary>
    public IReadOnlyList<StepRequest> Commands => _commands;

    /// <summary>
    /// Gets the commands that execute after the ProcessNext has been committed to storage (e.g., MovedToAltinnEvent).
    /// </summary>
    public IReadOnlyList<StepRequest> PostProcessNextCommittedCommands => _postProcessNextCommittedCommands;

    /// <summary>
    /// Creates command group for task start events.
    /// </summary>
    /// <param name="targetTaskId">The element ID of the task being started.</param>
    /// <param name="serviceTaskType">If this is a service task, the task type identifier. Otherwise null.</param>
    /// <param name="isInitialTaskStart">True if this is the first task start (process is starting), false for subsequent task transitions.</param>
    /// <param name="prefill">Prefill data for initial task start. Only relevant when isInitialTaskStart is true.</param>
    public static WorkflowCommandSet GetTaskStartSteps(
        string targetTaskId,
        ServiceTaskType? serviceTaskType,
        bool isInitialTaskStart,
        Dictionary<string, string>? prefill = null
    )
    {
        var group = new WorkflowCommandSet()
            .AddCommand(UnlockTaskData.Key)
            .AddCommand(WorkflowTaskStartLegacyHook.Key, new ProcessTaskStartLegacyHookPayload(prefill))
            .AddCommand(OnTaskStartingHook.Key)
            .AddCommand(CommonTaskInitialization.Key, new CommonTaskInitializationPayload(targetTaskId, prefill))
            .AddCommand(ProcessTaskStart.Key)
            .AddPostProcessNextCommittedCommand(MovedToAltinnEvent.Key);

        if (serviceTaskType is not null)
        {
            group.AddPostProcessNextCommittedCommand(
                ExecuteServiceTask.Key,
                new ExecuteServiceTaskPayload(serviceTaskType.Name)
            );

            if (serviceTaskType.Kind == ServiceTaskKind.ReplyServiceTask)
            {
                group.AddPostProcessNextCommittedReplyCommand(
                    ExecuteServiceTask.Key,
                    new ExecuteServiceTaskPayload(serviceTaskType.Name)
                );
            }
        }

        if (isInitialTaskStart)
        {
            group.AddPostProcessNextCommittedCommand(InstanceCreatedAltinnEvent.Key);
        }

        return group;
    }

    /// <summary>
    /// Creates command group for task end events.
    /// </summary>
    public static WorkflowCommandSet GetTaskEndSteps()
    {
        return new WorkflowCommandSet()
            .AddCommand(ProcessTaskEnd.Key)
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
            .AddCommand(ProcessTaskAbandon.Key)
            .AddCommand(OnTaskAbandonHook.Key)
            .AddCommand(AbandonTaskLegacyHook.Key);
    }

    /// <summary>
    /// Creates command group for process end events.
    /// </summary>
    public static WorkflowCommandSet GetProcessEndSteps()
    {
        // ProcessEndLegacyHook runs post-commit because IProcessEnd.End reads instance.Process.EndEvent,
        // which is only set when the process state is persisted. This matches the old ProcessEngine behavior
        // where RunAppDefinedProcessEndHandlers ran after HandleEventsAndUpdateStorage.
        return new WorkflowCommandSet()
            .AddCommand(OnWorkflowEndingHook.Key)
            .AddPostProcessNextCommittedCommand(ProcessEndLegacyHook.Key)
            .AddPostProcessNextCommittedCommand(CompletedAltinnEvent.Key);
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
    /// Adds a command that executes after the ProcessNext has been committed to storage via UpdateProcessState.
    /// </summary>
    private WorkflowCommandSet AddPostProcessNextCommittedCommand(
        string commandKey,
        CommandRequestPayload? payload = null
    )
    {
        _postProcessNextCommittedCommands.Add(CreateCommand(commandKey, payload));
        return this;
    }

    private WorkflowCommandSet AddPostProcessNextCommittedReplyCommand(
        string commandKey,
        CommandRequestPayload? payload = null
    )
    {
        _postProcessNextCommittedCommands.Add(CreateReplyCommand(commandKey, payload));
        return this;
    }

    private static StepRequest CreateCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new StepRequest { Command = new Command.AppCommand(CommandKey: commandKey, Payload: serializedPayload) };
    }

    private static StepRequest CreateReplyCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new StepRequest
        {
            Command = new Command.ReplyAppCommand(CommandKey: commandKey, Payload: serializedPayload),
        };
    }
}
