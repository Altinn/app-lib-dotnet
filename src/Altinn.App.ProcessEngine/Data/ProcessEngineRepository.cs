using Altinn.App.ProcessEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.App.ProcessEngine.Data;

internal sealed class ProcessEngineRepository : IProcessEngineRepository
{
    private readonly ProcessEngineDbContext _context;

    public ProcessEngineRepository(ProcessEngineDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessEngineJob?> GetJob(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context
            .Jobs.Include(j => j.Tasks)
            .FirstOrDefaultAsync(j => j.Identifier == identifier, cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessEngineJob>> GetJobsForInstance(
        InstanceInformation instanceInformation,
        CancellationToken cancellationToken = default
    )
    {
        var jobs = await _context
            .Jobs.Include(j => j.Tasks)
            .Where(j =>
                j.InstanceInformation.Org == instanceInformation.Org
                && j.InstanceInformation.App == instanceInformation.App
                && j.InstanceInformation.InstanceOwnerPartyId == instanceInformation.InstanceOwnerPartyId
                && j.InstanceInformation.InstanceGuid == instanceInformation.InstanceGuid
            )
            .ToListAsync(cancellationToken);

        return jobs;
    }

    public async Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(CancellationToken cancellationToken = default)
    {
        var incompleteStatuses = new[]
        {
            ProcessEngineItemStatus.Enqueued,
            ProcessEngineItemStatus.Processing,
            ProcessEngineItemStatus.Requeued,
        };

        var jobs = await _context
            .Jobs.Include(j => j.Tasks)
            .Where(j => incompleteStatuses.Contains(j.Status))
            .ToListAsync(cancellationToken);

        return jobs;
    }

    public async Task SaveJob(ProcessEngineJob job, CancellationToken cancellationToken = default)
    {
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default)
    {
        _context.Jobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteJob(string identifier, CancellationToken cancellationToken = default)
    {
        var job = await _context.Jobs.FindAsync([identifier], cancellationToken);
        if (job == null)
            return;

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProcessEngineTask?> GetTask(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);
    }

    public async Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
