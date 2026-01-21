using System.Text.Json;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.ProcessEngine;

internal sealed class RedisProcessingSessionCache : IProcessingSessionCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisProcessingSessionCache> _logger;
    private readonly TimeSpan _slidingExpiry = TimeSpan.FromMinutes(10);

    public RedisProcessingSessionCache(IDistributedCache cache, ILogger<RedisProcessingSessionCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string InstanceKey(string lockToken) => $"session:{lockToken}:instance";

    private static string DataKey(string lockToken, Guid dataElementId) => $"session:{lockToken}:data:{dataElementId}";

    public async Task<Instance?> GetInstance(string lockToken, CancellationToken ct = default)
    {
        try
        {
            var key = InstanceKey(lockToken);
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
            _logger.LogWarning(ex, "Failed to deserialize cached Instance for session {LockToken}", lockToken);
            _ = TryRemove(InstanceKey(lockToken), ct);
            return null;
        }
        catch (Exception ex)
        {
            // Redis error - treat as cache miss
            _logger.LogWarning(ex, "Redis error getting Instance for session {LockToken}", lockToken);
            return null;
        }
    }

    public async Task SetInstance(string lockToken, Instance instance, CancellationToken ct = default)
    {
        try
        {
            var key = InstanceKey(lockToken);
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
            _logger.LogWarning(ex, "Redis error setting Instance for session {LockToken}", lockToken);
        }
    }

    public async Task<ReadOnlyMemory<byte>?> GetBinaryData(
        string lockToken,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = DataKey(lockToken, dataElementId);
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
                "Redis error getting data {DataElementId} for session {LockToken}",
                dataElementId,
                lockToken
            );
            return null;
        }
    }

    public async Task SetBinaryData(
        string lockToken,
        Guid dataElementId,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default
    )
    {
        try
        {
            var key = DataKey(lockToken, dataElementId);
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
                "Redis error setting data {DataElementId} for session {LockToken}",
                dataElementId,
                lockToken
            );
        }
    }

    public async Task RemoveBinaryData(string lockToken, Guid dataElementId, CancellationToken ct = default)
    {
        try
        {
            var key = DataKey(lockToken, dataElementId);
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            // Redis error - swallow but log
            _logger.LogWarning(
                ex,
                "Redis error removing data {DataElementId} for session {LockToken}",
                dataElementId,
                lockToken
            );
        }
    }

    public Task InvalidateSession(string lockToken, CancellationToken ct = default)
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
