using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

public interface IAppEventOrchestrator
{
    /// <summary>
    /// Trigger the event StartProcessEvent
    /// All registered services implementing IAppEventReceiver interface will get the <see cref="Altinn.App.Core.Receivers.IAppEventReceiver.OnStartAppEvent"/> method invoked.
    /// </summary>
    /// <param name="startEvent">Start event to start</param>
    /// <param name="instance">Instance data</param>
    /// <returns></returns>
    public Task OnStartProcess(string startEvent, Instance instance);
    
    /// <summary>
    /// Trigger the event EndProcessEvent
    /// All registered services implementing IAppEventReceiver interface will get the <see cref="Altinn.App.Core.Receivers.IAppEventReceiver.OnEndAppEvent"/> method invoked.
    /// </summary>
    /// <param name="endEvent">End event to end</param>
    /// <param name="instance">Instance data</param>
    /// <returns></returns>
    public Task OnEndProcess(string endEvent, Instance instance);
    
    /// <summary>
    /// Trigger the event StartProcessTaskEvent after task started
    /// All registered services implementing IAppEventReceiver interface will get the <see cref="Altinn.App.Core.Receivers.ITaskEventReceiver.OnStartProcessTask"/> method invoked.
    /// </summary>
    /// <param name="taskId">task id of task started</param>
    /// <param name="instance">Instance data</param>
    /// <param name="prefill">Prefill data</param>
    /// <returns></returns>
    public Task OnStartProcessTask(string taskId, Instance instance, Dictionary<string, string> prefill);
    
    /// <summary>
    /// Trigger the event EndProcessTaskEvent
    /// </summary>
    /// <param name="taskId">task id of task ended</param>
    /// <param name="instance">Instance data</param>
    /// <returns></returns>
    public Task OnEndProcessTask(string taskId, Instance instance);
    
    /// <summary>
    /// Trigger the event AbandonProcessTaskEvent
    /// </summary>
    /// <param name="taskId">task id of task to abandon</param>
    /// <param name="instance">Instance data</param>
    /// <returns></returns>
    public Task OnAbandonProcessTask(string taskId, Instance instance);
}