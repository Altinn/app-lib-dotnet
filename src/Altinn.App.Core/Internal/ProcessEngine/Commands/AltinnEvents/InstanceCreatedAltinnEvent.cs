using Altinn.App.Core.Internal.Events;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal sealed class InstanceCreatedAltinnEvent : IProcessEngineCommand
{
    public static string Key => "InstanceCreatedAltinnEvent";

    public string GetKey() => Key;

    private readonly IEventsClient _eventsClient;

    public InstanceCreatedAltinnEvent(IEventsClient eventsClient)
    {
        _eventsClient = eventsClient;
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        Instance instance = parameters.InstanceDataMutator.Instance;

        try
        {
            await _eventsClient.AddEvent("app.instance.created", instance);

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
