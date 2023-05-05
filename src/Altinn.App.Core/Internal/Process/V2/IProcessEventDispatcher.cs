using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.V2;

public interface IProcessEventDispatcher
{
    Task<Instance> UpdateProcessAndDispatchEvents(Instance instance, Dictionary<string, string>? prefill, List<InstanceEvent> events);
    Task RegisterEventWithEventsComponent(Instance instance);
}