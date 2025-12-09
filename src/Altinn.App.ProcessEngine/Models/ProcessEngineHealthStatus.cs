namespace Altinn.App.ProcessEngine.Models;

[Flags]
public enum ProcessEngineHealthStatus
{
    Unknown = 0,
    Healthy = 1 << 0,
    Unhealthy = 1 << 1,
    Running = 1 << 2,
    Stopped = 1 << 3,
    QueueFull = 1 << 4,
    Disabled = 1 << 5,
    Idle = 1 << 6,
}
