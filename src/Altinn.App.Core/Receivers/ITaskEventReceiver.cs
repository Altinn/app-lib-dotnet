using Altinn.App.Core.Invokers;

namespace Altinn.App.Core.Receivers;

public interface ITaskEventReceiver
{
    /// <summary>
    /// Callback to app after task has been started.
    /// </summary>
    /// <returns></returns>
    public Task OnStartProcessTask(object? sender, TaskEventWithPrefillArgs eventArgs);
    
    /// <summary>
    /// Is called after the process task is ended. Method can update instance and data element metadata. 
    /// </summary>
    /// <param name="sender">class that invoked the event</param>
    /// <param name="eventArgs">taskId and instance data</param>
    public Task OnEndProcessTask(object? sender, TaskEventArgs eventArgs);
    
    /// <summary>
    /// Is called after the process task is abonded. Method can update instance and data element metadata. 
    /// </summary>
    /// <param name="sender">class that invoked the event</param>
    /// <param name="eventArgs">taskId and instance data</param>
    public Task OnAbandonProcessTask(object? sender, TaskEventArgs eventArgs);
    
}