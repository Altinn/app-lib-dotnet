namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessNextRequest(
    string CurrentElementId,
    string DesiredElementId,
    ProcessEngineActor ProcessEngineActor,
    IEnumerable<ProcessEngineCommandRequest> Tasks
)
{
    public ProcessEngineRequest ToProcessEngineRequest(InstanceInformation instanceInformation) =>
        new(
            $"{instanceInformation.InstanceGuid}/next/from-{CurrentElementId}-to-{DesiredElementId}",
            instanceInformation,
            ProcessEngineActor,
            Tasks
        );
};
