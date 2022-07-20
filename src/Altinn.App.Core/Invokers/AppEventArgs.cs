using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Invokers;

public class AppEventArgs
{
    public string Event { init; get; }
    public Instance Instance { init; get; }
}

public class TaskEventArgs
{
    public string TaskId { init; get; }
    public Instance Instance { init; get; }
}

public class TaskEventWithPrefillArgs
{
    public string TaskId { init; get; }
    public Instance Instance { init; get; }
    public Dictionary<string, string> Prefill { init; get; }
}