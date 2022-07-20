using Altinn.App.Core.Receivers;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

public class AppEventOrchestrator: IAppEventOrchestrator
{
    public delegate Task AsyncEventHandler<TEvent>(object? sender, TEvent eventArgs);
    
    public event AsyncEventHandler<AppEventArgs>? StartProcessEvent;
    
    public event AsyncEventHandler<AppEventArgs>? EndProcessEvent;
    
    public event AsyncEventHandler<TaskEventWithPrefillArgs>? StartProcessTaskEvent;
    
    public event AsyncEventHandler<TaskEventArgs>? EndProcessTaskEvent;
    
    public event AsyncEventHandler<TaskEventArgs>? AbandonProcessTaskEvent;
    
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