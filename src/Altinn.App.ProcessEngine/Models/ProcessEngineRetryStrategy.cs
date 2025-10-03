namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineRetryStrategy(
    ProcessEngineBackoffType BackoffType,
    TimeSpan Delay,
    int? MaxRetries = null,
    TimeSpan? MaxDelay = null
)
{
    public static ProcessEngineRetryStrategy Exponential(
        TimeSpan delay,
        int? maxRetries = null,
        TimeSpan? maxDelay = null
    ) => new(ProcessEngineBackoffType.Exponential, delay, maxRetries, maxDelay);

    public static ProcessEngineRetryStrategy Linear(
        TimeSpan delay,
        int? maxRetries = null,
        TimeSpan? maxDelay = null
    ) => new(ProcessEngineBackoffType.Linear, delay, maxRetries, maxDelay);

    public static ProcessEngineRetryStrategy Constant(TimeSpan delay, int? maxRetries = null) =>
        new(ProcessEngineBackoffType.Constant, delay, maxRetries);

    public static ProcessEngineRetryStrategy Fixed(TimeSpan delay, int? maxRetries = null) =>
        Constant(delay, maxRetries);

    public static ProcessEngineRetryStrategy None() => new(ProcessEngineBackoffType.Constant, TimeSpan.Zero, 0);
}
