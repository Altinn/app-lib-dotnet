namespace Altinn.App.Core.Internal.Process.ProcessLock;

internal sealed class ProcessLockOptions
{
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);
}
