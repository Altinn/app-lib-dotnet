using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.AltinnEvents;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.ProcessEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskAbandon;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskEnd;
using Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskStart;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Defines a group of commands that should be executed for a process event.
/// </summary>
internal sealed class WorkflowCommandSet
{
    private readonly List<ProcessEngineCommandRequest> _commands = [];
    private readonly List<ProcessEngineCommandRequest> _postProcessNextCommittedCommands = [];

    /// <summary>
    /// Gets the main commands for this event. UpdateProcessState will be added after these.
    /// </summary>
    public IReadOnlyList<ProcessEngineCommandRequest> Commands => _commands;

    /// <summary>
    /// Gets the commands that execute after the ProcessNext has been committed to storage (e.g., MovedToAltinnEvent).
    /// </summary>
    public IReadOnlyList<ProcessEngineCommandRequest> PostProcessNextCommittedCommands =>
        _postProcessNextCommittedCommands;

    /// <summary>
    /// Creates command group for task start events.
    /// </summary>
    /// <param name="serviceTaskType">If this is a service task, the task type identifier. Otherwise null.</param>
    /// <param name="isInitialTaskStart">True if this is the first task start (process is starting), false for subsequent task transitions.</param>
    public static WorkflowCommandSet GetTaskStartSteps(string? serviceTaskType, bool isInitialTaskStart)
    {
        var group = new WorkflowCommandSet()
            .AddCommand(UnlockTaskData.Key)
            .AddCommand(WorkflowTaskStartLegacyHook.Key)
            .AddCommand(OnTaskStartingHook.Key)
            .AddCommand(CommonTaskInitialization.Key)
            .AddCommand(WorkflowTaskStart.Key)
            .AddPostProcessNextCommittedCommand(MovedToAltinnEvent.Key);

        if (serviceTaskType is not null)
        {
            group.AddPostProcessNextCommittedCommand(
                ExecuteServiceTask.Key,
                new ExecuteServiceTaskPayload(serviceTaskType)
            );
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
            .AddCommand(WorkflowTaskEnd.Key)
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
            .AddCommand(WorkflowTaskAbandon.Key)
            .AddCommand(OnTaskAbandonHook.Key)
            .AddCommand(AbandonTaskLegacyHook.Key);
    }

    /// <summary>
    /// Creates command group for process end events.
    /// </summary>
    public static WorkflowCommandSet GetProcessEndSteps()
    {
        return new WorkflowCommandSet()
            .AddCommand(OnWorkflowEndingHook.Key)
            .AddCommand(WorkflowEndLegacyHook.Key)
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

    private static ProcessEngineCommandRequest CreateCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        string? serializedPayload = CommandPayloadSerializer.Serialize(payload);
        return new ProcessEngineCommandRequest
        {
            Command = new ProcessEngineCommand.AppCommand(CommandKey: commandKey, Payload: serializedPayload),
        };
    }
}
