using System.Collections.Concurrent;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// A thread-safe memory cache that refreshes the values when they are about to expire
/// </summary>
/// <typeparam name="TKey">The cache key</typeparam>
/// <typeparam name="TValue">The cached value</typeparam>
internal sealed class RefreshCache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The number of items in the cache
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Gets a collection containing the keys in the cache
    /// </summary>
    public ICollection<TKey> Keys => _cache.Keys;

    /// <summary>
    /// Gets a collection containing the entries in the cache
    /// </summary>
    public ICollection<CacheEntry> Values => _cache.Values;

    /// <summary>
    /// Record to store the cached values with their expiry and refresh times
    /// </summary>
    public record CacheEntry(DateTimeOffset Expiry, DateTimeOffset RefreshAt, TValue Value);

    /// <summary>
    /// The collection that holds all cached items
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();

    private const int NumLocks = 32;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _refetchBeforeExpiry;
    private readonly int _maxCacheEntries;
    private readonly SemaphoreSlim[] _locks;

    /// <summary>
    /// Instantiates a new <see cref="RefreshCache{TKey,TValue}"/> instance.
    /// </summary>
    /// <param name="timeProvider">The timeprovider service.</param>
    /// <param name="refetchBeforeExpiry">The threshold at which to refresh cache entries <em>before</em> they expire.</param>
    /// <param name="maxCacheEntries">
    /// Maximum entries to store in cache. If evictions are required, FIFO policy will be used.
    /// To allow unlimited cache entries, set this value to zero.
    /// </param>
    public RefreshCache(TimeProvider timeProvider, TimeSpan refetchBeforeExpiry, int maxCacheEntries = 0)
    {
        _refetchBeforeExpiry = refetchBeforeExpiry;
        _timeProvider = timeProvider;
        _maxCacheEntries = maxCacheEntries;

        // Populate locks array with _numLocks slots
        _locks = Enumerable.Range(0, NumLocks).Select(_ => new SemaphoreSlim(1)).ToArray();
    }

    /// <summary>
    /// Retrieves a value from the cache, or creates it if not present or expired.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="valueFactory">Factory method to create the value (if not in cache).</param>
    /// <param name="lifetimeFactory">Factory method to calculate the lifetime of the cached value.</param>
    /// <param name="postProcessCallback">Optional post processing callback (for metrics, etc)</param>
    public async Task<TValue> GetOrCreate(
        TKey key,
        Func<Task<TValue>> valueFactory,
        Func<TValue, TimeSpan> lifetimeFactory,
        Action<TValue?, CacheResultType>? postProcessCallback = default
    )
    {
        var lockNo = (key.GetHashCode() & 0x7fffffff) % NumLocks;
        var semaphore = _locks[lockNo];
        await semaphore.WaitAsync();

        try
        {
            var now = _timeProvider.GetUtcNow();
            TValue? result;

            if (_cache.TryGetValue(key, out var entry))
            {
                // If the entry is still valid (with margin), return it
                if (entry.RefreshAt > now)
                {
                    postProcessCallback?.Invoke(entry.Value, CacheResultType.Cached);
                    return entry.Value;
                }

                // Kick off a new valueFactory task
                var cacheTask = GenerateItemAndUpdateCache(
                    key,
                    valueFactory,
                    lifetimeFactory,
                    now,
                    postProcessCallback
                );

                // Return previous value without awaiting value if it is still valid
                if (entry.Expiry > now)
                {
                    postProcessCallback?.Invoke(entry.Value, CacheResultType.Refreshed);
                    return entry.Value;
                }

                // Await the new value if the previous value has expired
                result = await cacheTask;
                postProcessCallback?.Invoke(result, CacheResultType.Expired);
                return result;
            }

            // No previous value found, so we need to await the task from the initializer cache
            result = await GenerateItemAndUpdateCache(key, valueFactory, lifetimeFactory, now, postProcessCallback);
            postProcessCallback?.Invoke(result, CacheResultType.New);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves a value from the cache, or creates it if not present or expired.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="valueFactory">Factory function to create the value (if not in cache).</param>
    /// <param name="lifetime">The lifetime of the cached value.</param>
    /// <param name="postProcessCallback">Optional post processing callback (for metrics, etc)</param>
    public async Task<TValue> GetOrCreate(
        TKey key,
        Func<Task<TValue>> valueFactory,
        TimeSpan? lifetime = default,
        Action<TValue?, CacheResultType>? postProcessCallback = default
    )
    {
        return await GetOrCreate(
            key: key,
            valueFactory: valueFactory,
            lifetimeFactory: _ => lifetime ?? DateTimeOffset.MaxValue - _timeProvider.GetUtcNow(),
            postProcessCallback: postProcessCallback
        );
    }

    private async Task<TValue> GenerateItemAndUpdateCache(
        TKey key,
        Func<Task<TValue>> valueFactory,
        Func<TValue, TimeSpan> lifetimeFactory,
        DateTimeOffset now,
        Action<TValue?, CacheResultType>? processingErrorCallback = default
    )
    {
        try
        {
            var value = await valueFactory();
            var lifetime = lifetimeFactory(value);
            var expiry = now + lifetime;
            var refreshAt = expiry - _refetchBeforeExpiry;
            var cacheEntry = new CacheEntry(expiry, refreshAt, value);

            // Remove expired items
            foreach (var expiredItem in _cache.Where(x => x.Value.Expiry <= now))
            {
                _cache.TryRemove(expiredItem);
            }

            // Store the new value
            _cache.AddOrUpdate(key, cacheEntry, (_, _) => cacheEntry);

            // Too many items? Prune old ones
            if (_maxCacheEntries > 0 && _cache.Count > _maxCacheEntries)
            {
                var oldestItems = GetOverflowItems_FIFO(_maxCacheEntries);
                foreach (var oldItem in oldestItems)
                {
                    _cache.TryRemove(oldItem);
                }
            }

            return value;
        }
        catch (Exception)
        {
            processingErrorCallback?.Invoke(default, CacheResultType.Error);
            throw;
        }
    }

    /// <summary>
    /// Helper: Gets the overflowing cache entries. Eg. the items that should be removed because cache size is over its limit
    /// </summary>
    /// <param name="cacheSizeLimit">The cache size limit</param>
    private IEnumerable<KeyValuePair<TKey, CacheEntry>> GetOverflowItems_FIFO(int cacheSizeLimit)
    {
        return _cache.OrderBy(x => x.Value.Expiry).Take(_cache.Count - cacheSizeLimit);
    }
}

/// <summary>
/// The type of cache result an operation returned
/// </summary>
public enum CacheResultType
{
    /// <summary>
    /// The item was cached and valid, returned as-is.
    /// </summary>
    Cached,

    /// <summary>
    /// The item as cached and valid, but about to expire.
    /// Returned as-is, but kicked off a refresh call via factory method.
    /// </summary>
    Refreshed,

    /// <summary>
    /// The item was in the cache, but had expired.
    /// A new item was generated via factory method.
    /// </summary>
    Expired,

    /// <summary>
    /// The item was not found in the cache.
    /// A new item was generated via factory method.
    /// </summary>
    New,

    /// <summary>
    /// An error was raised while executing the factory method for a new item.
    /// </summary>
    Error
}
