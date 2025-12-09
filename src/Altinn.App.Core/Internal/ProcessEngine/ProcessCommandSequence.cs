using Altinn.App.Core.Internal.ProcessEngine.Commands;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// A sequence of process commands that enforces UpdateProcessState is added after each event's commands.
/// </summary>
internal sealed class ProcessCommandSequence
{
    private readonly List<string> _commands = new();

    /// <summary>
    /// Adds commands for a process event. Automatically appends UpdateProcessState after the event commands
    /// to ensure the state change is committed to Storage.
    /// </summary>
    /// <param name="eventCommands">The commands to execute for this event</param>
    public void AddEventCommands(List<string> eventCommands)
    {
        if (eventCommands.Count > 0)
        {
            _commands.AddRange(eventCommands);
            _commands.Add(UpdateProcessState.Key);
        }
    }

    /// <summary>
    /// Adds a single command without UpdateProcessState.
    /// Use this for commands that are part of an event's execution (like ExecuteServiceTask)
    /// rather than state transitions.
    /// </summary>
    /// <param name="command">The command to add</param>
    public void AddCommand(string command)
    {
        _commands.Add(command);
    }

    /// <summary>
    /// Returns the final command sequence as a list.
    /// </summary>
    public List<string> ToList() => _commands;
}
