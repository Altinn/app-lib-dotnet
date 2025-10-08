namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Represents a single task to be processed by the process engine.
/// </summary>
public sealed record ProcessEngineTaskRequest(
    string Identifier,
    ProcessEngineTaskCommand Command,
    DateTimeOffset? StartTime = null,
    ProcessEngineRetryStrategy? RetryStrategy = null
);
