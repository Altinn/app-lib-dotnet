using System.Text.Json;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.WorkflowEngine.Caching;

internal sealed class RedisLockScopedInstanceCache : ILockScopedInstanceCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisLockScopedInstanceCache> _logger;
    private readonly TimeSpan _slidingExpiry = TimeSpan.FromMinutes(10);

    public RedisLockScopedInstanceCache(IDistributedCache cache, ILogger<RedisLockScopedInstanceCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // Key format: lock:{lockToken}:instance:{instanceGuid}:instance
    // Includes instanceGuid for easier debugging and isolation
    private static string InstanceKey(string lockToken, Guid instanceGuid) =>
        $"lock:{lockToken}:instance:{instanceGuid}:instance";

    private static string DataKey(string lockToken, Guid instanceGuid, Guid dataElementId) =>
        $"lock:{lockToken}:instance:{instanceGuid}:data:{dataElementId}";

    public async Task<Instance?> GetInstance(string lockToken, Guid instanceGuid, CancellationToken ct = default)
    {
        try
        {
            var key = InstanceKey(lockToken, instanceGuid);
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null)
                return null;

            // Refresh TTL (sliding expiration)
            _ = RefreshTtl(key, ct);

            return JsonSerializer.Deserialize<Instance>(bytes);
        }
        catch (JsonException ex)
        {
            // Corrupt data - treat as cache miss, optionally delete
            _logger.LogWarning(
                ex,
                "Failed to deserialize cached Instance for lock {LockToken}, instance {InstanceGuid}",
                lockToken,
                instanceGuid
            );
            _ = TryRemove(InstanceKey(lockToken, instanceGuid), ct);
            return null;
        }
        catch (Exception ex)
        {
            // Redis error - treat as cache miss
            _logger.LogWarning(
                ex,
                "Redis error getting Instance for lock {LockToken}, instance {InstanceGuid}",
                lockToken,
                instanceGuid
            );
            return null;
        }
    }

    public async Task SetInstance(
        string lockToken,
        Guid instanceGuid,
        Instance instance,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = InstanceKey(lockToken, instanceGuid);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(instance);
            await _cache.SetAsync(
                key,
                bytes,
                new DistributedCacheEntryOptions { SlidingExpiration = _slidingExpiry },
                ct
            );
        }
        catch (Exception ex)
        {
            // Redis error - swallow but log
            _logger.LogWarning(
                ex,
                "Redis error setting Instance for lock {LockToken}, instance {InstanceGuid}",
                lockToken,
                instanceGuid
            );
        }
    }

    public async Task<ReadOnlyMemory<byte>?> GetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = DataKey(lockToken, instanceGuid, dataElementId);
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null)
                return null;

            // Refresh TTL (sliding expiration)
            _ = RefreshTtl(key, ct);

            return new ReadOnlyMemory<byte>(bytes);
        }
        catch (Exception ex)
        {
            // Redis error - treat as cache miss
            _logger.LogWarning(
                ex,
                "Redis error getting data {DataElementId} for lock {LockToken}, instance {InstanceGuid}",
                dataElementId,
                lockToken,
                instanceGuid
            );
            return null;
        }
    }

    public async Task SetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = DataKey(lockToken, instanceGuid, dataElementId);
            await _cache.SetAsync(
                key,
                data.ToArray(),
                new DistributedCacheEntryOptions { SlidingExpiration = _slidingExpiry },
                ct
            );
        }
        catch (Exception ex)
        {
            // Redis error - swallow but log
            _logger.LogWarning(
                ex,
                "Redis error setting data {DataElementId} for lock {LockToken}, instance {InstanceGuid}",
                dataElementId,
                lockToken,
                instanceGuid
            );
        }
    }

    public async Task RemoveBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = DataKey(lockToken, instanceGuid, dataElementId);
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            // Redis error - swallow but log
            _logger.LogWarning(
                ex,
                "Redis error removing data {DataElementId} for lock {LockToken}, instance {InstanceGuid}",
                dataElementId,
                lockToken,
                instanceGuid
            );
        }
    }

    public Task InvalidateSession(string lockToken, Guid instanceGuid, CancellationToken ct = default)
    {
        // IDistributedCache doesn't support pattern deletion.
        // Rely on sliding expiration TTL for cleanup.
        return Task.CompletedTask;
    }

    private async Task RefreshTtl(string key, CancellationToken ct)
    {
        try
        {
            // IDistributedCache.RefreshAsync updates sliding expiration
            await _cache.RefreshAsync(key, ct);
        }
        catch
        {
            // Ignore TTL refresh failures
        }
    }

    private async Task TryRemove(string key, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch
        {
            // Ignore removal failures
        }
    }
}
