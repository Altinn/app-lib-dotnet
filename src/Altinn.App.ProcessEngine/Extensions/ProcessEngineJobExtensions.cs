using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Extensions;

internal static class ProcessEngineJobExtensions
{
    public static IOrderedEnumerable<ProcessEngineTask> OrderedTasks(this ProcessEngineJob job) =>
        job.Tasks.OrderBy(t => t.ProcessingOrder);

    public static IOrderedEnumerable<ProcessEngineTask> OrderedIncompleteTasks(this ProcessEngineJob job) =>
        job.Tasks.Where(x => !x.Status.IsDone()).OrderBy(x => x.ProcessingOrder);

    public static ProcessEngineItemStatus OverallStatus(this ProcessEngineJob job)
    {
        if (job.Tasks.All(t => t.Status == ProcessEngineItemStatus.Completed))
        {
            return ProcessEngineItemStatus.Completed;
        }

        if (job.Tasks.Any(t => t.Status == ProcessEngineItemStatus.Failed))
        {
            return ProcessEngineItemStatus.Failed;
        }

        if (job.Tasks.Any(t => t.Status == ProcessEngineItemStatus.Canceled))
        {
            return ProcessEngineItemStatus.Canceled;
        }

        return job.Tasks.Any(t => t.Status != ProcessEngineItemStatus.Enqueued)
            ? ProcessEngineItemStatus.Processing
            : ProcessEngineItemStatus.Enqueued;
    }
}
