using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data;

/// <summary>
/// No-op repository for in-memory only ProcessEngine operation.
/// Simulates database latency for performance testing.
/// </summary>
internal sealed class InMemoryProcessEngineRepository : IProcessEngineRepository
{
    public async Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default)
    {
        await SimulateDatabaseDelay(cancellationToken);
        return [];
    }

    public async Task SaveJob(ProcessEngineJob job, CancellationToken cancellationToken = default) =>
        await SimulateDatabaseDelay(cancellationToken);

    public async Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default) =>
        await SimulateDatabaseDelay(cancellationToken);

    public async Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default) =>
        await SimulateDatabaseDelay(cancellationToken);

    private static async Task SimulateDatabaseDelay(CancellationToken cancellationToken = default) =>
        await Task.Delay(Random.Shared.Next(50, 500), cancellationToken);
}
