using System.Text.Json;
using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.Core.Models.Process;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// Builds a sequence of process commands with proper ordering.
/// Enforces that UpdateProcessState is added between event commands and follow-up commands.
/// </summary>
internal sealed class ProcessCommandSequence
{
    private readonly List<ProcessEngineCommandRequest> _commands = [];
    private readonly ProcessStateChange _processStateChange;

    public ProcessCommandSequence(ProcessStateChange processStateChange)
    {
        _processStateChange = processStateChange;
    }

    /// <summary>
    /// Adds a command group for a instance event.
    /// </summary>
    /// <param name="eventCommandGroup">The command group defining what to execute</param>
    public void AddEventCommandGroup(ProcessEventCommandGroup eventCommandGroup)
    {
        // Add main commands
        _commands.AddRange(eventCommandGroup.Commands);

        // Add UpdateProcessState to commit the state change to Storage
        _commands.Add(CreateUpdateProcessStateCommand());

        // Commands that should run after the process next is saved to storage. Execute service task, publish altinn event, etc.
        _commands.AddRange(eventCommandGroup.PostProcessNextCommittedCommands);
    }

    /// <summary>
    /// Adds a single command without UpdateProcessState.
    /// Use this for additional commands that are part of an event's execution (like ExecuteServiceTask)
    /// rather than state transitions.
    /// </summary>
    /// <param name="command">The command to add</param>
    public void AddCommand(ProcessEngineCommandRequest command)
    {
        _commands.Add(command);
    }

    /// <summary>
    /// Returns the final command sequence as a list.
    /// </summary>
    public List<ProcessEngineCommandRequest> ToList() => _commands;

    private ProcessEngineCommandRequest CreateUpdateProcessStateCommand()
    {
        string? processStateChangeMetadata = JsonSerializer.Serialize(_processStateChange);
        return new ProcessEngineCommandRequest
        {
            Command = new ProcessEngineCommand.AppCommand(
                CommandKey: UpdateProcessState.Key,
                Metadata: processStateChangeMetadata
            ),
        };
    }
}
