using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineJob
{
    public ProcessEngineItemStatus Status { get; set; }
    public required AppIdentifier AppIdentifier { get; init; }
    public required Instance Instance { get; init; }
    public required IReadOnlyList<ProcessEngineTask> Tasks { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdate { get; set; }

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            AppIdentifier = request.AppIdentifier,
            Instance = request.Instance,
            Tasks = request.Tasks.Select(ProcessEngineTask.FromRequest).ToList(),
        };
};
