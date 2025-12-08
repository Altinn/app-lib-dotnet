using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessLock;

internal sealed partial class ProcessLocker(
    ProcessLockClient client,
    IOptions<ProcessLockOptions> options,
    ILogger<ProcessLocker> logger
)
{
    public async Task<IAsyncDisposable> AcquireAsync(Instance instance)
    {
        var instanceIdParts = instance.Id.Split('/');
        if (
            instanceIdParts.Length != 2
            || !int.TryParse(instanceIdParts[0], out var instanceOwnerPartyId)
            || !Guid.TryParse(instanceIdParts[1], out var instanceGuid)
        )
        {
            throw new ArgumentException("Instance ID was not in the expected format.");
        }

        var lockId = await client.AcquireProcessLock(instanceGuid, instanceOwnerPartyId, options.Value.Expiration);

        LogLockAcquired(logger, lockId);

        return new ProcessLock(instanceGuid, instanceOwnerPartyId, lockId, client, logger);
    }

    private sealed partial class ProcessLock(
        Guid instanceGuid,
        int instanceOwnerPartyId,
        Guid lockId,
        ProcessLockClient client,
        ILogger logger
    ) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await client.ReleaseProcessLock(instanceGuid, instanceOwnerPartyId, lockId);

                LogLockReleased(logger, lockId);

                return;
            }
            catch (Exception e)
            {
                LogLockReleaseFailed(logger, lockId, e);
            }
        }
    }

    [LoggerMessage(1, LogLevel.Debug, "Failed to acquire process lock.")]
    private static partial void LogLockAcquisitionFailed(ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "Acquired process lock with id: {LockId}")]
    private static partial void LogLockAcquired(ILogger logger, Guid lockId);

    [LoggerMessage(3, LogLevel.Debug, "Released process lock with id: {LockId}")]
    private static partial void LogLockReleased(ILogger logger, Guid lockId);

    [LoggerMessage(4, LogLevel.Error, "Failed to release process lock with id: {LockId}")]
    private static partial void LogLockReleaseFailed(ILogger logger, Guid lockId, Exception e);
}
