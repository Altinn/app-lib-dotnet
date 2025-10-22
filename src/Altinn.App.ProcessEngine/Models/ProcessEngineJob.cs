namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob
{
    public ProcessEngineItemStatus Status { get; set; }
    public required string Identifier { get; init; }
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdate { get; set; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            Identifier = request.JobIdentifier,
            ProcessEngineActor = request.ProcessEngineActor,
            Tasks = request
                .Tasks.Select((x, i) => ProcessEngineTask.FromRequest(x, request.ProcessEngineActor, i))
                .ToList(),
        };

    public override string ToString() => $"{nameof(ProcessEngineJob)}: {Identifier} ({Status})";

    public bool Equals(ProcessEngineJob? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();
};
