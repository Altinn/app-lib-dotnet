namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Settings for the Process Engine.
/// </summary>
public sealed record ProcessEngineSettings
{
    /// <summary>
    /// The API key used to authenticate requests to the Process Engine.
    /// </summary>
    public string ApiKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The total number of concurrent tasks that can be processed.
    /// </summary>
    public int QueueCapacity { get; set; } = 10000;

    /// <summary>
    /// The default timeout for task execution.
    /// </summary>
    public TimeSpan DefaultTaskExecutionTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// The default retry strategy for tasks.
    /// </summary>
    public ProcessEngineRetryStrategy DefaultTaskRetryStrategy { get; set; } =
        ProcessEngineRetryStrategy.Exponential(
            baseInterval: TimeSpan.FromSeconds(1),
            maxRetries: int.MaxValue,
            maxDelay: TimeSpan.FromSeconds(35)
        );
}
