namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob : ProcessEngineItem
{
    public required ProcessEngineActor Actor { get; init; }
    public required InstanceInformation InstanceInformation { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            Key = request.JobKey,
            InstanceInformation = request.InstanceInformation,
            Actor = request.Actor,
            Tasks = request
                .Commands.Select((cmd, i) => ProcessEngineTask.FromRequest(request.JobKey, cmd, request.Actor, i))
                .ToList(),
        };

    public override string ToString() => $"[{GetType().Name}] {Key} ({Status})";
};
