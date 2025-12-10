using Altinn.App.Core.Infrastructure.Clients.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessLock;

internal sealed partial class ProcessLocker(
    ProcessLockClient client,
    IOptions<ProcessLockOptions> options,
    ILogger<ProcessLocker> logger,
    IHttpContextAccessor httpContextAccessor
) : IAsyncDisposable
{
    private readonly HttpContext _httpContext =
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext cannot be null.");

    private ProcessLock? _lock;

    public async ValueTask LockAsync()
    {
        if (_lock is not null)
        {
            return;
        }

        var (instanceOwnerPartyId, instanceGuid) =
            GetInstanceIdentifiers() ?? throw new InvalidOperationException("Unable to extract instance identifiers.");

        var lockId = await client.AcquireProcessLock(instanceGuid, instanceOwnerPartyId, options.Value.Expiration);

        LogLockAcquired(logger, lockId);

        _lock = new ProcessLock(instanceGuid, instanceOwnerPartyId, lockId);
    }

    private (int instanceOwnerPartyId, Guid instanceGuid)? GetInstanceIdentifiers()
    {
        var routeValues = _httpContext.Request.RouteValues;

        if (
            routeValues.TryGetValue("instanceOwnerPartyId", out var partyIdObj)
            && routeValues.TryGetValue("instanceGuid", out var guidObj)
            && int.TryParse(partyIdObj?.ToString(), out var partyId)
            && Guid.TryParse(guidObj?.ToString(), out var guid)
        )
        {
            return (partyId, guid);
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_lock is null)
        {
            return;
        }

        try
        {
            await client.ReleaseProcessLock(_lock.InstanceGuid, _lock.InstanceOwnerPartyId, _lock.LockId);
        }
        catch (Exception e)
        {
            LogLockReleaseFailed(logger, _lock.LockId, e);
            return;
        }

        LogLockReleased(logger, _lock.LockId);

        _lock = null;
    }

    private sealed record ProcessLock(Guid InstanceGuid, int InstanceOwnerPartyId, Guid LockId);

    [LoggerMessage(1, LogLevel.Debug, "Failed to acquire process lock.")]
    private static partial void LogLockAcquisitionFailed(ILogger logger);

    [LoggerMessage(2, LogLevel.Debug, "Acquired process lock with id: {LockId}")]
    private static partial void LogLockAcquired(ILogger logger, Guid lockId);

    [LoggerMessage(3, LogLevel.Debug, "Released process lock with id: {LockId}")]
    private static partial void LogLockReleased(ILogger logger, Guid lockId);

    [LoggerMessage(4, LogLevel.Error, "Failed to release process lock with id: {LockId}")]
    private static partial void LogLockReleaseFailed(ILogger logger, Guid lockId, Exception e);
}
