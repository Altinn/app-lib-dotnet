using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data;

internal interface IProcessEngineRepository
{
    Task<ProcessEngineJob?> GetJob(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessEngineJob>> GetJobsForInstance(
        InstanceInformation instanceInformation,
        CancellationToken cancellationToken = default
    );
    Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default);
    Task SaveJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default);
    Task DeleteJob(string identifier, CancellationToken cancellationToken = default);

    Task<ProcessEngineTask?> GetTask(string identifier, CancellationToken cancellationToken = default);
    Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default);
}
