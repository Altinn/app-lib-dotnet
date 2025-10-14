using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine;

internal partial class ProcessEngine
{
    public async Task<ProcessEngineResponse> EnqueueJob(
        ProcessEngineRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("(public) Enqueuing job {JobIdentifier}", request.JobIdentifier);

        if (!request.IsValid())
            return ProcessEngineResponse.Rejected("Invalid request");

        if (HasQueuedJob(request.JobIdentifier))
            return ProcessEngineResponse.Rejected("Duplicate request");

        if (_mainLoopTask is null)
            return ProcessEngineResponse.Rejected("Process engine is not running. Did you call Start()?");

        var enabled = await _enabledStatusHistory.Latest() ?? await ShouldRun(cancellationToken);
        if (!enabled)
            return ProcessEngineResponse.Rejected(
                "Process engine is currently inactive. Did you call the right instance?"
            );

        await AcquireQueueSlot(cancellationToken); // Only acquire slots for public requests
        await EnqueueJob(ProcessEngineJob.FromRequest(request), true, cancellationToken);

        return ProcessEngineResponse.Accepted();
    }

    private async Task EnqueueJob(
        ProcessEngineJob job,
        bool updateDatabase = true,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("(internal) Enqueuing job {Job}. Update database: {UpdateDb}", job, updateDatabase);

        // TODO: persist to database if `updateDatabase` is true
        await Task.CompletedTask;

        _inbox[job.Identifier] = job;
    }

    public bool HasQueuedJob(string jobIdentifier)
    {
        return _inbox.ContainsKey(jobIdentifier);
    }

    private Task PopulateJobsFromStorage(CancellationToken cancellationToken)
    {
        // TODO: Populate the queue from the database. This must be a resilient call to db
        _logger.LogDebug("Populating jobs from storage");
        return Task.CompletedTask;
    }

    private Task UpdateJobInStorage(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        // TODO: Should we update the `Instance` with something here too? Like if the job has failed, etc
        // TODO: This must be a resilient call to db
        _logger.LogDebug("Updating job in storage: {Job}", job);
        return Task.CompletedTask;
    }

    private Task UpdateTaskInStorage(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        // TODO: This must be a resilient call to db
        _logger.LogDebug("Updating task in storage: {Task}", task);
        return Task.CompletedTask;
    }
}
