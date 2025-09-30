namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineSettings
{
    // TODO: Perhaps a key to protect the API, shared with the Altinn.App.* projects?
    public Guid ApiKey { get; set; } = Guid.NewGuid();
    public int QueueCapacity { get; set; } = 1000;
    public TimeSpan DefaultTaskExecutionTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public ProcessEngineRetryStrategy DefaultTaskRetryStrategy { get; set; } =
        new(ProcessEngineBackoffType.Exponential, TimeSpan.FromSeconds(1), int.MaxValue);
}
