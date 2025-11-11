namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob : ProcessEngineDatabaseItem
{
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public required InstanceInformation InstanceInformation { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            Identifier = request.JobIdentifier,
            InstanceInformation = request.InstanceInformation,
            CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow, // TODO: Hmm...
            ProcessEngineActor = request.ProcessEngineActor,
            Tasks = request
                .Tasks.Select(
                    (task, i) =>
                        ProcessEngineTask.FromRequest(request.JobIdentifier, task, request.ProcessEngineActor, i)
                )
                .ToList(),
        };

    public override string ToString() => $"[{GetType().Name}] {Identifier} ({Status})";
};
