using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob
{
    public ProcessEngineItemStatus Status { get; set; }
    public required string Identifier { get; init; }
    public required Instance Instance { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdate { get; set; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            Identifier = request.JobIdentifier,
            Instance = request.Instance,
            Tasks = request.Tasks.Select(ProcessEngineTask.FromRequest).ToList(),
        };

    public override string ToString() => $"{nameof(ProcessEngineJob)}: {Identifier} ({Status})";

    public bool Equals(ProcessEngineJob? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();
};
