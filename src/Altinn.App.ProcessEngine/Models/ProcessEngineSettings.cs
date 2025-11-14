using Altinn.App.ProcessEngine.Constants;

namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Settings for the Process Engine.
/// </summary>
public sealed record ProcessEngineSettings
{
    /// <summary>
    /// The API key used to authenticate requests to the Process Engine.
    /// </summary>
    public string ApiKey { get; set; } = Defaults.ApiKey;

    /// <summary>
    /// The total number of concurrent tasks that can be processed.
    /// </summary>
    public int QueueCapacity { get; set; } = Defaults.QueueCapacity;

    /// <summary>
    /// The default timeout for task execution.
    /// </summary>
    public TimeSpan DefaultTaskExecutionTimeout { get; set; } = Defaults.DefaultTaskExecutionTimeout;

    /// <summary>
    /// The default retry strategy for tasks.
    /// </summary>
    public ProcessEngineRetryStrategy DefaultTaskRetryStrategy { get; set; } = Defaults.DefaultTaskRetryStrategy;
}
