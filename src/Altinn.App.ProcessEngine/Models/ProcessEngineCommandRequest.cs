namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Represents a single task to be processed by the process engine.
/// </summary>
public sealed record ProcessEngineCommandRequest(
    InstanceInformation InstanceInformation,
    ProcessEngineCommand Command,
    DateTimeOffset? StartTime = null,
    ProcessEngineRetryStrategy? RetryStrategy = null,
    DateTimeOffset? CreatedAt = null
);
