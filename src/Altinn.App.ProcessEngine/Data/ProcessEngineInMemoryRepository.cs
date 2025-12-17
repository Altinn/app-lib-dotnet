using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data;

/// <summary>
/// In-memory repository for ProcessEngine operations.
/// Simulates database latency for performance testing.
/// </summary>
internal sealed class ProcessEngineInMemoryRepository : IProcessEngineRepository
{
    public async Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default)
    {
        await SimulateDatabaseDelay(cancellationToken);
        return [];
    }

    public async Task<ProcessEngineJob> AddJob(ProcessEngineJob job, CancellationToken cancellationToken = default)
    {
        await SimulateDatabaseDelay(cancellationToken);
        return job;
    }

    public async Task<ProcessEngineJob> UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default)
    {
        await SimulateDatabaseDelay(cancellationToken);
        return job;
    }

    public async Task<ProcessEngineTask> UpdateTask(
        ProcessEngineTask task,
        CancellationToken cancellationToken = default
    )
    {
        await SimulateDatabaseDelay(cancellationToken);
        return task;
    }

    private static async Task SimulateDatabaseDelay(CancellationToken cancellationToken = default) =>
        await Task.Delay(Random.Shared.Next(50, 500), cancellationToken);
}
