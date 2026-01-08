using Altinn.App.Core.Internal.Events;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal sealed class MovedToAltinnEvent : IProcessEngineCommand
{
    public static string Key => "MovedToAltinnEvent";

    public string GetKey() => Key;

    private readonly IEventsClient _eventsClient;

    public MovedToAltinnEvent(IEventsClient eventsClient)
    {
        _eventsClient = eventsClient;
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        Instance instance = parameters.InstanceDataMutator.Instance;

        try
        {
            if (string.IsNullOrWhiteSpace(instance.Process?.CurrentTask?.ElementId))
                throw new InvalidOperationException(
                    "Current task is not set on instance process. Cannot raise movedTo event."
                );

            await _eventsClient.AddEvent(
                $"app.instance.process.movedTo.{instance.Process.CurrentTask.ElementId}",
                instance
            );

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
