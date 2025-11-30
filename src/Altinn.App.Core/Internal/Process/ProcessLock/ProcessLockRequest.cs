namespace Altinn.App.Core.Internal.Process.ProcessLock;

internal sealed class ProcessLockRequest
{
    public int TtlSeconds { get; set; }
}
