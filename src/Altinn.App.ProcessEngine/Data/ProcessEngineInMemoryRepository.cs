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

    public async Task<ProcessEngineJob> AddJob(
        ProcessEngineJobRequest jobRequest,
        CancellationToken cancellationToken = default
    )
    {
        await SimulateDatabaseDelay(cancellationToken);
        return ProcessEngineJob.FromRequest(jobRequest);
    }

    public async Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default) =>
        await SimulateDatabaseDelay(cancellationToken);

    public async Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default) =>
        await SimulateDatabaseDelay(cancellationToken);

    private static async Task SimulateDatabaseDelay(CancellationToken cancellationToken = default) =>
        await Task.Delay(Random.Shared.Next(50, 500), cancellationToken);
}
