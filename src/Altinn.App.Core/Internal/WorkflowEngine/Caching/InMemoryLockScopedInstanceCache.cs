using System.Collections.Concurrent;
using System.Text.Json;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Caching;

/// <summary>
/// In-memory implementation of <see cref="ILockScopedInstanceCache"/> for testing.
/// Thread-safe and uses ConcurrentDictionary for storage.
/// </summary>
internal sealed class InMemoryLockScopedInstanceCache : ILockScopedInstanceCache
{
    private readonly ConcurrentDictionary<string, byte[]> _cache = new();

    private static string InstanceKey(string lockToken, Guid instanceGuid) =>
        $"lock:{lockToken}:instance:{instanceGuid}:instance";

    private static string DataKey(string lockToken, Guid instanceGuid, Guid dataElementId) =>
        $"lock:{lockToken}:instance:{instanceGuid}:data:{dataElementId}";

    public Task<Instance?> GetInstance(string lockToken, Guid instanceGuid, CancellationToken ct = default)
    {
        var key = InstanceKey(lockToken, instanceGuid);
        if (_cache.TryGetValue(key, out var bytes))
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<Instance>(bytes));
            }
            catch (JsonException)
            {
                // Corrupt data - treat as cache miss
                _cache.TryRemove(key, out _);
                return Task.FromResult<Instance?>(null);
            }
        }
        return Task.FromResult<Instance?>(null);
    }

    public Task SetInstance(string lockToken, Guid instanceGuid, Instance instance, CancellationToken ct = default)
    {
        var key = InstanceKey(lockToken, instanceGuid);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(instance);
        _cache[key] = bytes;
        return Task.CompletedTask;
    }

    public Task<ReadOnlyMemory<byte>?> GetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        var key = DataKey(lockToken, instanceGuid, dataElementId);
        if (_cache.TryGetValue(key, out var bytes))
        {
            return Task.FromResult<ReadOnlyMemory<byte>?>(new ReadOnlyMemory<byte>(bytes));
        }
        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    public Task SetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default
    )
    {
        var key = DataKey(lockToken, instanceGuid, dataElementId);
        _cache[key] = data.ToArray();
        return Task.CompletedTask;
    }

    public Task RemoveBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        var key = DataKey(lockToken, instanceGuid, dataElementId);
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task InvalidateSession(string lockToken, Guid instanceGuid, CancellationToken ct = default)
    {
        // Remove all keys for this lock token and instance
        var prefix = $"lock:{lockToken}:instance:{instanceGuid}:";
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all cached data. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
}
