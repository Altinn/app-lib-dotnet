namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Defines a retry strategy for process engine tasks.
/// </summary>
/// <param name="BackoffType">The type of backoff to use.</param>
/// <param name="BaseInterval">The base interval between attempts. The actual delay grows or stays constant based on the backoff type.</param>
/// <param name="MaxRetries">The maximum allowed number of retries before giving up.</param>
/// <param name="MaxDelay">The maximum allowed delay between retries. Useful for linear and exponential types.</param>
public sealed record ProcessEngineRetryStrategy(
    ProcessEngineBackoffType BackoffType,
    TimeSpan BaseInterval,
    int? MaxRetries = null,
    TimeSpan? MaxDelay = null
)
{
    /// <summary>
    /// Creates an exponential backoff retry strategy.
    /// </summary>
    public static ProcessEngineRetryStrategy Exponential(
        TimeSpan baseInterval,
        int? maxRetries = null,
        TimeSpan? maxDelay = null
    ) => new(ProcessEngineBackoffType.Exponential, baseInterval, maxRetries, maxDelay);

    /// <summary>
    /// Creates a linear backoff retry strategy.
    /// </summary>
    public static ProcessEngineRetryStrategy Linear(
        TimeSpan baseInterval,
        int? maxRetries = null,
        TimeSpan? maxDelay = null
    ) => new(ProcessEngineBackoffType.Linear, baseInterval, maxRetries, maxDelay);

    /// <summary>
    /// Creates a constant backoff retry strategy.
    /// </summary>
    public static ProcessEngineRetryStrategy Constant(TimeSpan interval, int? maxRetries = null) =>
        new(ProcessEngineBackoffType.Constant, interval, maxRetries, interval);

    /// <summary>
    /// Alias for <see cref="Constant"/>
    /// </summary>
    public static ProcessEngineRetryStrategy Fixed(TimeSpan intervalDelay, int? maxRetries = null) =>
        Constant(intervalDelay, maxRetries);

    /// <summary>
    /// Creates a retry strategy with no retries.
    /// </summary>
    public static ProcessEngineRetryStrategy None() =>
        new(ProcessEngineBackoffType.Constant, TimeSpan.Zero, 0, TimeSpan.Zero);
}
