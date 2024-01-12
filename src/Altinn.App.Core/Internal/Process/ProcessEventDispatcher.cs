using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Default implementation of the process event dispatcher
/// </summary>
class ProcessEventDispatcher(
    IInstanceClient instanceClient,
    IInstanceEventClient instanceEventClient,
    IEventsClient eventsClient,
    IOptions<AppSettings> appSettings,
    ILogger<ProcessEventDispatcher> logger
) : IProcessEventDispatcher
{

    /// <inheritdoc/>
    public async Task<Instance> DispatchToStorage(Instance instance, List<InstanceEvent>? events)
    {
        // need to update the instance process and then the instance in case appbase has changed it, e.g. endEvent sets status.archived
        Instance updatedInstance = await instanceClient.UpdateProcess(instance);
        await DispatchProcessEventsToStorage(updatedInstance, events);

        // remember to get the instance anew since AppBase can have updated a data element or stored something in the database.
        updatedInstance = await instanceClient.GetInstance(updatedInstance);

        return updatedInstance;
    }

    /// <inheritdoc/>
    public async Task RegisterEventWithEventsComponent(Instance instance)
    {
        if (appSettings.Value.RegisterEventsWithEventsComponent)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(instance.Process.CurrentTask?.ElementId))
                {
                    await eventsClient.AddEvent(
                        $"app.instance.process.movedTo.{instance.Process.CurrentTask.ElementId}", instance);
                }
                else if (instance.Process.EndEvent != null)
                {
                    await eventsClient.AddEvent("app.instance.process.completed", instance);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Exception when sending event with the Events component");
            }
        }
    }

    private async Task DispatchProcessEventsToStorage(Instance instance, List<InstanceEvent>? events)
    {
        string org = instance.Org;
        string app = instance.AppId.Split("/")[1];

        if (events != null)
        {
            foreach (InstanceEvent instanceEvent in events)
            {
                instanceEvent.InstanceId = instance.Id;
                await instanceEventClient.SaveInstanceEvent(instanceEvent, org, app);
            }
        }
    }
}