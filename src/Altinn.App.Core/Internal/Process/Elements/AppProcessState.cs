#nullable enable
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.Elements;

/// <summary>
/// Extended representation of a status object that holds the process state of an application instance.
/// The process is defined by the application's process specification BPMN file.
/// </summary>
public class AppProcessState: ProcessState
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public AppProcessState()
    {
    }

    /// <summary>
    /// Constructor that takes a ProcessState object and copies the values.
    /// </summary>
    /// <param name="processState"></param>
    public AppProcessState(ProcessState processState)
    {
        Started = processState.Started;
        StartEvent = processState.StartEvent;
        CurrentTask = new AppProcessElementInfo(processState.CurrentTask);
        Ended = processState.Ended;
        EndEvent = processState.EndEvent;
    }
    
    /// <summary>
    /// Gets or sets a status object containing the task info of the currentTask of an ongoing process.
    /// </summary>
    public new AppProcessElementInfo CurrentTask { get; set; }
}
