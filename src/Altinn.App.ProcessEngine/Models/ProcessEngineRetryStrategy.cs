namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineRetryStrategy(
    ProcessEngineBackoffType BackoffType,
    TimeSpan Delay,
    int? MaxRetries = null,
    TimeSpan? MaxDelay = null
);
