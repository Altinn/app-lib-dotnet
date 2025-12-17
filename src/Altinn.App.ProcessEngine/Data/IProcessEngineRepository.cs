using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data;

internal interface IProcessEngineRepository
{
    Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default);
    Task<ProcessEngineJob> AddJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task<ProcessEngineJob> UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task<ProcessEngineTask> UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default);
}
