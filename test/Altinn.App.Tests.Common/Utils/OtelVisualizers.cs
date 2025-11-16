using System.Diagnostics;
using System.Text;
using OpenTelemetry.Logs;

namespace Altinn.App.Tests.Common.Utils;

public static class OtelVisualizers
{
    public static string VisualizeActivities(
        IEnumerable<Activity> activities,
        IEnumerable<LogRecord> logs,
        bool includeDuration = true,
        int initialIndent = 0
    )
    {
        var logsByActivityId = logs.GroupBy(l => l.SpanId)
            .ToDictionary(k => k.Key, v => v.OrderBy(l => l.Timestamp).ToList());
        var activityByParentId = activities
            .GroupBy(a => a.ParentSpanId)
            .ToDictionary(k => k.Key, g => g.OrderBy(a => a.StartTimeUtc).ToList());

        // Find root activities: those whose parent span ID is not in our collection
        var rootActivities = activityByParentId
            .Where(kvp => !activityByParentId.ContainsKey(kvp.Key))
            .SelectMany(kvp => kvp.Value);

        var sb = new StringBuilder();
        int rootActivityCount = 0;
        foreach (var activity in rootActivities)
        {
            rootActivityCount++;
            VisualizeActivity(sb, activityByParentId, logsByActivityId, activity, initialIndent, includeDuration);
        }

        if (rootActivityCount == 0)
        {
            sb.AppendLine("No root activities found.");
        }

        var remainingLogs = logsByActivityId.Values.SelectMany(l => l).OrderBy(l => l.Timestamp).ToList();
        if (remainingLogs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Orphaned Logs:");
            foreach (var log in remainingLogs)
            {
                sb.AppendLine($"[{log.LogLevel}] {log.FormattedMessage}");
            }
        }
        return sb.ToString();
    }

    private static void VisualizeActivity(
        StringBuilder sb,
        Dictionary<ActivitySpanId, List<Activity>> activityByParentId,
        Dictionary<ActivitySpanId, List<LogRecord>> logsByActivityId,
        Activity activity,
        int indent,
        bool includeDuration
    )
    {
        sb.Append(' ', indent);
        if (includeDuration)
        {
            sb.AppendLine($"{activity.DisplayName}  [Duration: {activity.Duration.TotalMilliseconds} ms]");
        }
        else
        {
            sb.AppendLine($"{activity.DisplayName}");
        }
        foreach (var (key, value) in activity.Tags)
        {
            sb.Append(' ', indent + 1);
            sb.AppendLine($"{key}: {value}");
        }

        if (activityByParentId.TryGetValue(activity.SpanId, out var children))
        {
            foreach (var child in children)
            {
                VisualizeActivity(sb, activityByParentId, logsByActivityId, child, indent + 2, includeDuration);
            }
        }

        if (logsByActivityId.TryGetValue(activity.SpanId, out var logs) && logs.Count > 0)
        {
            sb.Append(' ', indent + 1);
            sb.AppendLine("Logs:");
            foreach (var log in logs)
            {
                sb.Append(' ', indent + 2);
                sb.AppendLine($"[{log.LogLevel}] {log.FormattedMessage}");
            }
            logsByActivityId.Remove(activity.SpanId);
        }
    }
}
