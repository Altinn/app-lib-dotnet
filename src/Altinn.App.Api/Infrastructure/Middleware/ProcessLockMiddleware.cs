using Altinn.App.Core.Helpers;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware that ensures only one request can proceed at a time by acquiring a lock.
/// Only applies to endpoints decorated with <see cref="EnableProcessLockAttribute"/>.
/// </summary>
internal sealed partial class ProcessLockMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly ProcessLockClient _processClient;
    private readonly ProcessLockOptions _options;

    public ProcessLockMiddleware(
        RequestDelegate next,
        ILogger<ProcessLockMiddleware> logger,
        IOptions<ProcessLockOptions> options,
        ProcessLockClient processClient
    )
    {
        _next = next;
        _logger = logger;
        _processClient = processClient;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<EnableProcessLockAttribute>() is null)
        {
            await _next(context);
            return;
        }

        var (instanceOwnerPartyId, instanceGuid) =
            GetInstanceIdentifiers(context)
            ?? throw new InvalidOperationException("Unable to extract instance identifiers.");
        Guid? lockId = null;

        try
        {
            try
            {
                lockId = await _processClient.AcquireProcessLock(
                    instanceGuid,
                    instanceOwnerPartyId,
                    _options.Expiration
                );

                LogLockAcquired(_logger, lockId.Value);
            }
            catch (PlatformHttpException e)
            {
                LogLockAcquisitionFailed(_logger);
                var problem = TypedResults.Problem(
                    detail: e.Message,
                    statusCode: e.Response.IsSuccessStatusCode ? 500 : (int)e.Response.StatusCode,
                    title: "Failed to acquire lock."
                );

                await problem.ExecuteAsync(context);

                return;
            }

            await _next(context);
        }
        finally
        {
            if (lockId is not null)
            {
                try
                {
                    await _processClient.ReleaseProcessLock(instanceGuid, instanceOwnerPartyId, lockId.Value);

                    LogLockReleased(_logger, lockId.Value);
                }
                catch (Exception e)
                {
                    LogLockReleaseFailed(_logger, lockId.Value, e);
                }
            }
        }
    }

    private static (int instanceOwnerPartyId, Guid instanceGuid)? GetInstanceIdentifiers(HttpContext context)
    {
        var routeData = context.GetRouteData();

        if (
            routeData.Values.TryGetValue("instanceOwnerPartyId", out var partyIdObj)
            && routeData.Values.TryGetValue("instanceGuid", out var guidObj)
            && int.TryParse(partyIdObj?.ToString(), out var partyId)
            && Guid.TryParse(guidObj?.ToString(), out var guid)
        )
        {
            return (partyId, guid);
        }

        return null;
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
