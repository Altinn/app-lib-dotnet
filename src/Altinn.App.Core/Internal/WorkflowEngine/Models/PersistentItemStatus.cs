namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Represents the status of a persistent workflow item.
/// </summary>
public enum PersistentItemStatus
{
    /// <summary>The item has been enqueued for processing.</summary>
    Enqueued = 0,

    /// <summary>The item is currently being processed.</summary>
    Processing = 1,

    /// <summary>The item has been requeued after a previous attempt.</summary>
    Requeued = 2,

    /// <summary>The item has completed successfully.</summary>
    Completed = 3,

    /// <summary>The item has failed.</summary>
    Failed = 4,

    /// <summary>The item has been canceled.</summary>
    Canceled = 5,
}
