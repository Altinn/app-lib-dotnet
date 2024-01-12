using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process
{
    public interface IProcessEventHandlingDelegator
    {
        Task HandleEvents(Instance instance, Dictionary<string, string>? prefill, List<InstanceEvent>? events);
    }
}