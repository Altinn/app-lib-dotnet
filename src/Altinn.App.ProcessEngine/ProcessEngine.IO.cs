using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine;

internal partial class ProcessEngine
{
    public async Task<ProcessEngineResponse> EnqueueJob(
        ProcessEngineRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Enqueuing job {JobIdentifier}", request.JobKey);

        if (!request.IsValid())
            return ProcessEngineResponse.Rejected($"Invalid request: {request}");

        if (HasDuplicateJob(request.JobKey))
            return ProcessEngineResponse.Rejected(
                "Duplicate request. A job with the same identifier is already being processed"
            );

        if (HasQueuedJobForInstance(request.InstanceInformation))
            return ProcessEngineResponse.Rejected(
                "A job for this instance is already processing. Concurrency is currently not supported"
            );

        if (_mainLoopTask is null)
            return ProcessEngineResponse.Rejected("Process engine is not running. Did you call Start()?");

        var enabled = await _isEnabledHistory.Latest() ?? await ShouldRun(cancellationToken);
        if (!enabled)
            return ProcessEngineResponse.Rejected(
                "Process engine is currently inactive. Did you call the right instance?"
            );

        await AcquireQueueSlot(cancellationToken); // Only acquire slots for public requests
        await EnqueueJob(
            job: ProcessEngineJob.FromRequest(request),
            updateDatabase: true,
            cancellationToken: cancellationToken
        );

        return ProcessEngineResponse.Accepted();
    }

    private async Task EnqueueJob(
        ProcessEngineJob job,
        bool updateDatabase = true,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogTrace("Enqueuing job {Job}. Update database: {UpdateDb}", job, updateDatabase);

        if (updateDatabase)
        {
            await _repository.SaveJob(job, cancellationToken);
            _logger.LogDebug("Job {JobIdentifier} persisted to database", job.Key);
        }

        _inbox[job.Key] = job;
    }

    public bool HasDuplicateJob(string jobIdentifier)
    {
        return _inbox.ContainsKey(jobIdentifier);
    }

    public bool HasQueuedJobForInstance(InstanceInformation instanceInformation)
    {
        return _inbox.Values.Any(x => x.InstanceInformation.Equals(instanceInformation));
    }

    public ProcessEngineJob? GetJobForInstance(InstanceInformation instanceInformation)
    {
        return _inbox.Values.FirstOrDefault(x => x.InstanceInformation.Equals(instanceInformation));
    }

    private async Task PopulateJobsFromStorage(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Populating jobs from storage");

        try
        {
            IReadOnlyList<ProcessEngineJob> incompleteJobs = await _repository.GetIncompleteJobs(cancellationToken);

            foreach (var job in incompleteJobs)
            {
                // TODO: Not sure about this logic...
                // Only add if not already in memory to avoid duplicates
                if (_inbox.TryAdd(job.Key, job))
                    _logger.LogDebug("Restored job {JobIdentifier} from database", job.Key);
            }

            _logger.LogInformation("Populated {JobCount} jobs from storage", incompleteJobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to populate jobs from storage after all retries. Continuing with in-memory only operation"
            );
        }
    }

    private async Task UpdateJobInStorage(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        // TODO: Should we update the `Instance` with something here too? Like if the job has failed, etc
        _logger.LogDebug("Updating job in storage: {Job}", job);

        try
        {
            job.UpdatedAt = _timeProvider.GetUtcNow();
            await _repository.UpdateJob(job, cancellationToken);

            _logger.LogTrace("Job {JobIdentifier} updated in database", job.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job {JobIdentifier} in database after all retries", job.Key);
            // Continue processing even if database update fails
        }
    }

    private async Task UpdateTaskInStorage(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating task in storage: {Task}", task);

        try
        {
            task.UpdatedAt = _timeProvider.GetUtcNow();
            await _repository.UpdateTask(task, cancellationToken);

            _logger.LogTrace("Task {TaskIdentifier} updated in database", task.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {TaskIdentifier} in database after all retries", task.Key);
            // Continue processing even if database update fails
        }
    }
}
