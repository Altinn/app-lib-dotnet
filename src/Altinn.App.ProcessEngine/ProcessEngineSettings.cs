namespace Altinn.App.ProcessEngine;

public sealed record ProcessEngineSettings
{
    public Guid ApiKey { get; set; } = Guid.NewGuid();
    public int QueueCapacity { get; set; } = 1000;
}
