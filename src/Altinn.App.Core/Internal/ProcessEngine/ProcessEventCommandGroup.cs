using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// Defines a group of commands that should be executed for a process event.
/// </summary>
internal sealed class ProcessEventCommandGroup
{
    private readonly List<ProcessEngineCommandRequest> _commands = new();
    private readonly List<ProcessEngineCommandRequest> _followUpCommands = new();

    /// <summary>
    /// Gets the main commands for this event. UpdateProcessState will be added after these.
    /// </summary>
    public IReadOnlyList<ProcessEngineCommandRequest> Commands => _commands;

    /// <summary>
    /// Gets the commands that execute after the ProcessNext has been committed to storage (e.g., MovedToAltinnEvent).
    /// </summary>
    public IReadOnlyList<ProcessEngineCommandRequest> PostProcessNextCommittedCommands => _followUpCommands;

    /// <summary>
    /// Adds a command to the main sequence.
    /// </summary>
    public ProcessEventCommandGroup AddCommand(string commandKey, string? metadata = null)
    {
        _commands.Add(CreateCommand(commandKey, metadata));
        return this;
    }

    /// <summary>
    /// Adds a command that executes after the ProcessNext has been committed to storage via UpdateProcessState.
    /// </summary>
    public ProcessEventCommandGroup AddPostProcessNextCommittedCommand(string commandKey, string? metadata = null)
    {
        _followUpCommands.Add(CreateCommand(commandKey, metadata));
        return this;
    }

    private static ProcessEngineCommandRequest CreateCommand(string commandKey, string? metadata = null)
    {
        return new ProcessEngineCommandRequest
        {
            Command = new ProcessEngineCommand.AppCommand(CommandKey: commandKey, Metadata: metadata),
        };
    }

    /// <summary>
    /// Creates command group for task start events.
    /// </summary>
    public static ProcessEventCommandGroup ForTaskStart()
    {
        return new ProcessEventCommandGroup()
            .AddCommand(UnlockTaskData.Key)
            .AddCommand(StartTaskLegacyHook.Key)
            .AddCommand(OnTaskStartingHook.Key)
            .AddCommand(CommonTaskInitialization.Key)
            .AddCommand(ProcessTaskStart.Key)
            .AddPostProcessNextCommittedCommand(MovedToAltinnEvent.Key);
    }

    /// <summary>
    /// Creates command group for task end events.
    /// </summary>
    public static ProcessEventCommandGroup ForTaskEnd()
    {
        return new ProcessEventCommandGroup()
            .AddCommand(ProcessTaskEnd.Key)
            .AddCommand(CommonTaskFinalization.Key)
            .AddCommand(EndTaskLegacyHook.Key)
            .AddCommand(OnTaskEndingHook.Key)
            .AddCommand(LockTaskData.Key);
    }

    /// <summary>
    /// Creates command group for task abandon events.
    /// </summary>
    public static ProcessEventCommandGroup ForTaskAbandon()
    {
        return new ProcessEventCommandGroup()
            .AddCommand(ProcessTaskAbandon.Key)
            .AddCommand(OnTaskAbandonHook.Key)
            .AddCommand(AbandonTaskLegacyHook.Key);
    }

    /// <summary>
    /// Creates command group for process end events.
    /// </summary>
    public static ProcessEventCommandGroup ForProcessEnd()
    {
        return new ProcessEventCommandGroup()
            .AddCommand(OnProcessEndingHook.Key)
            .AddCommand(ProcessEndLegacyHook.Key)
            .AddPostProcessNextCommittedCommand(CompletedAltinnEvent.Key);
    }
}
