using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data;

internal interface IProcessEngineRepository
{
    Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default);
    Task SaveJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default);
}
