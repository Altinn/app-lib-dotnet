using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// Defines a group of commands that should be executed for a process event.
/// </summary>
internal sealed class ProcessEventCommands
{
    private readonly List<ProcessEngineCommandRequest> _commands = new();
    private readonly List<ProcessEngineCommandRequest> _postProcessNextCommittedCommands = new();

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
    /// Adds a command to the main sequence.
    /// </summary>
    public ProcessEventCommands AddCommand(string commandKey, CommandRequestPayload? payload = null)
    {
        _commands.Add(CreateCommand(commandKey, payload));
        return this;
    }

    /// <summary>
    /// Adds a command that executes after the ProcessNext has been committed to storage via UpdateProcessState.
    /// </summary>
    public ProcessEventCommands AddPostProcessNextCommittedCommand(
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

    /// <summary>
    /// Creates command group for task start events.
    /// </summary>
    /// <param name="serviceTaskType">If this is a service task, the task type identifier. Otherwise null.</param>
    /// <param name="isInitialTaskStart">True if this is the first task start (process is starting), false for subsequent task transitions.</param>
    public static ProcessEventCommands GetTaskStartCommands(string? serviceTaskType, bool isInitialTaskStart)
    {
        var group = new ProcessEventCommands()
            .AddCommand(UnlockTaskData.Key)
            .AddCommand(ProcessTaskStartLegacyHook.Key)
            .AddCommand(OnTaskStartingHook.Key)
            .AddCommand(CommonTaskInitialization.Key)
            .AddCommand(ProcessTaskStart.Key)
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
    public static ProcessEventCommands GetTaskEndCommands()
    {
        return new ProcessEventCommands()
            .AddCommand(ProcessTaskEnd.Key)
            .AddCommand(CommonTaskFinalization.Key)
            .AddCommand(EndTaskLegacyHook.Key)
            .AddCommand(OnTaskEndingHook.Key)
            .AddCommand(LockTaskData.Key);
    }

    /// <summary>
    /// Creates command group for task abandon events.
    /// </summary>
    public static ProcessEventCommands GetTaskAbandonCommands()
    {
        return new ProcessEventCommands()
            .AddCommand(ProcessTaskAbandon.Key)
            .AddCommand(OnTaskAbandonHook.Key)
            .AddCommand(AbandonTaskLegacyHook.Key);
    }

    /// <summary>
    /// Creates command group for process end events.
    /// </summary>
    public static ProcessEventCommands GetProcessEndCommands()
    {
        return new ProcessEventCommands()
            .AddCommand(OnProcessEndingHook.Key)
            .AddCommand(ProcessEndLegacyHook.Key)
            .AddPostProcessNextCommittedCommand(CompletedAltinnEvent.Key);
    }
}
