namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineSettings
{
    public string ApiKey { get; set; } = Guid.NewGuid().ToString();
    public int QueueCapacity { get; set; } = 10000;
    public TimeSpan DefaultTaskExecutionTimeout { get; set; } = TimeSpan.FromSeconds(100);

    public ProcessEngineRetryStrategy DefaultTaskRetryStrategy { get; set; } =
        new(
            BackoffType: ProcessEngineBackoffType.Exponential,
            Delay: TimeSpan.FromSeconds(1),
            MaxDelay: TimeSpan.FromSeconds(30)
        );
}
