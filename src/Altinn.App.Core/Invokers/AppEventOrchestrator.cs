using Altinn.App.Core.Receivers;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

public class AppEventOrchestrator: IAppEventOrchestrator
{
    /// <summary>
    /// Delegate for handling async events.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public delegate Task AsyncEventHandler<TEvent>(object? sender, TEvent eventArgs);
    
    /// <summary>
    ///  Event for app start event. This event is never raised at the moment. Invokes <see cref="Altinn.App.Core.Receivers.IAppEventReceiver.OnStartAppEvent"/>
    /// </summary>
    public event AsyncEventHandler<AppEventArgs>? StartProcessEvent;
    
    /// <summary>
    /// Event raised when the process for an instance is ended. Invokes <see cref="Altinn.App.Core.Receivers.IAppEventReceiver.OnEndAppEvent"/>
    /// </summary>
    public event AsyncEventHandler<AppEventArgs>? EndProcessEvent;
    
    /// <summary>
    /// Event raised after process task has been started. Invokes <see cref="Altinn.App.Core.Receivers.ITaskEventReceiver.OnStartProcessTask"/>
    /// </summary>
    public event AsyncEventHandler<TaskEventWithPrefillArgs>? StartProcessTaskEvent;
    
    /// <summary>
    /// Event raised after the process task has ended. Method can update instance and data element metadata. Invokes <see cref="Altinn.App.Core.Receivers.ITaskEventReceiver.OnEndProcessTask"/>
    /// </summary>
    public event AsyncEventHandler<TaskEventArgs>? EndProcessTaskEvent;
    
    /// <summary>
    /// Event raised after the process task is abandoned. Method can update instance and data element metadata. Invokes <see cref="Altinn.App.Core.Receivers.ITaskEventReceiver.OnAbandonProcessTask"/> 
    /// </summary>
    public event AsyncEventHandler<TaskEventArgs>? AbandonProcessTaskEvent;
    
    /// <summary>
    /// Instansiate process orchestrator. All registered services implementing IAppEventReceiver and ITaskEventReceiver will be added to the events logic.
    /// </summary>
    /// <param name="appEventReceivers"></param>
    /// <param name="taskEventReceivers"></param>
    public AppEventOrchestrator(IEnumerable<IAppEventReceiver> appEventReceivers, IEnumerable<ITaskEventReceiver> taskEventReceivers)
    {
        foreach (var appEventReceiver in appEventReceivers)
        {
            StartProcessEvent += appEventReceiver.OnStartAppEvent;
            EndProcessEvent += appEventReceiver.OnEndAppEvent;
        }

        foreach (var taskEventReceiver in taskEventReceivers)
        {
            StartProcessTaskEvent += taskEventReceiver.OnStartProcessTask;
            EndProcessTaskEvent += taskEventReceiver.OnEndProcessTask;
            AbandonProcessTaskEvent += taskEventReceiver.OnAbandonProcessTask;
        }
    }

    /// <inheritdoc />
    public async Task OnStartProcess(string startEvent, Instance instance)
    {
        if (StartProcessEvent is not null)
        {
            AppEventArgs appEventArgs = new AppEventArgs(){
                Event = startEvent,
                Instance = instance
            };
            await StartProcessEvent.Invoke(this, appEventArgs);
        }
    }

    /// <inheritdoc />
    public async Task OnEndProcess(string endEvent, Instance instance)
    {
        if (EndProcessEvent is not null)
        {
            AppEventArgs appEventArgs = new AppEventArgs(){
                Event = endEvent,
                Instance = instance
            };
            await EndProcessEvent.Invoke(this, appEventArgs);
        }
    }

    /// <inheritdoc />
    public async Task OnStartProcessTask(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        if (StartProcessTaskEvent is not null)
        {
            TaskEventWithPrefillArgs taskEventWithPrefillArgs = new TaskEventWithPrefillArgs(){
                TaskId = taskId,
                Instance = instance,
                Prefill = prefill
            };
            await StartProcessTaskEvent.Invoke(this, taskEventWithPrefillArgs);
        }
    }

    /// <inheritdoc />
    public async Task OnEndProcessTask(string taskId, Instance instance)
    {
        if (EndProcessTaskEvent is not null)
        {
            TaskEventArgs taskEventArgs = new TaskEventArgs(){
                TaskId = taskId,
                Instance = instance
            };
            await EndProcessTaskEvent.Invoke(this, taskEventArgs);
        }
    }

    /// <inheritdoc />
    public async Task OnAbandonProcessTask(string taskId, Instance instance)
    {
        if (AbandonProcessTaskEvent is not null)
        {
            TaskEventArgs taskEventArgs = new TaskEventArgs(){
                TaskId = taskId,
                Instance = instance
            };
            await AbandonProcessTaskEvent.Invoke(this, taskEventArgs);
        }
    }
}
