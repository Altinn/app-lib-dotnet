using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

/// <summary>
/// Event arguments for events on an instance 
/// </summary>
public class AppEventArgs
{
    /// <summary>
    /// Event id
    /// </summary>
    public string Event { init; get; }
    /// <summary>
    /// Instance data
    /// </summary>
    public Instance Instance { init; get; }
}

/// <summary>
/// Event arguments for events on a process task
/// </summary>
public class TaskEventArgs
{
    /// <summary>
    /// Id of task in process
    /// </summary>
    public string TaskId { init; get; }
    /// <summary>
    /// Instance data
    /// </summary>
    public Instance Instance { init; get; }
}


/// <summary>
/// Event arguments for events on a process task with prefill data
/// </summary>
public class TaskEventWithPrefillArgs
{
    /// <summary>
    /// Id of task in process
    /// </summary>
    public string TaskId { init; get; }
    /// <summary>
    /// Instance data
    /// </summary>
    public Instance Instance { init; get; }
    /// <summary>
    /// Prefill data
    /// </summary>
    public Dictionary<string, string> Prefill { init; get; }
}
