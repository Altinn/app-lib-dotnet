namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob : ProcessEngineDatabaseItem
{
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            Identifier = request.JobIdentifier,
            CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow, // TODO: Hmm...
            ProcessEngineActor = request.ProcessEngineActor,
            Tasks = request
                .Tasks.Select((x, i) => ProcessEngineTask.FromRequest(x, request.ProcessEngineActor, i))
                .ToList(),
        };
};
