using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Extensions;

internal static class ProcessEngineJobExtensions
{
    public static IOrderedEnumerable<ProcessEngineTask> OrderedTasks(this ProcessEngineJob job) =>
        job.Tasks.OrderBy(t => t.ProcessingOrder);

    public static IOrderedEnumerable<ProcessEngineTask> OrderedIncompleteTasks(this ProcessEngineJob job) =>
        job.Tasks.Where(x => !x.Status.IsDone()).OrderBy(x => x.ProcessingOrder);
}
