using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

public interface IAppEventOrchestrator
{
    public Task OnStartProcess(string startEvent, Instance instance);
    
    public Task OnEndProcess(string endEvent, Instance instance);
    
    public Task OnStartProcessTask(string taskId, Instance instance, Dictionary<string, string> prefill);
    
    public Task OnEndProcessTask(string taskId, Instance instance);
    
    public Task OnAbandonProcessTask(string taskId, Instance instance);
}