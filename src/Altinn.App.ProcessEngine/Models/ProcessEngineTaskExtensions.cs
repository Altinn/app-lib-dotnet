namespace Altinn.App.ProcessEngine.Models;

internal static class ProcessEngineTaskExtensions
{
    public static bool IsReadyForExecution(this ProcessEngineTask task, DateTimeOffset now)
    {
        if (task.BackoffUntil.HasValue && task.BackoffUntil > now)
            return false;

        if (task.StartTime.HasValue && task.StartTime > now)
            return false;

        return true;
    }

    public static bool IsReadyForExecution(this ProcessEngineTask task, TimeProvider timeProvider) =>
        IsReadyForExecution(task, timeProvider.GetUtcNow());
}
