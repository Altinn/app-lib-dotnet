using System.Collections.Concurrent;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// A therad-safe memory cache that refreshes the values when they are about to expire
/// </summary>
/// <typeparam name="TKey">The cache key</typeparam>
/// <typeparam name="TValue">The cached value</typeparam>
internal sealed class LazyRefreshCache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The number of items in the cache
    /// </summary>
    public int Count => _valueCache.Count;

    /// <summary>
    /// Gets a collection containing the keys in the cache
    /// </summary>
    public ICollection<TKey> Keys => _valueCache.Keys;

    /// <summary>
    /// Gets a collection containing the entries in the cache
    /// </summary>
    public ICollection<CacheEntry> Values => _valueCache.Values;

    /// <summary>
    /// Value type to store the cached values with their expiry time in the concurrent dictionary
    /// </summary>
    public record CacheEntry(DateTimeOffset Expiry, DateTimeOffset RefreshAt, TValue Value);

    /// <summary>
    /// Cache the tasks that fetch the values, so that we can await them in a thread-safe manner
    /// </summary>
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _taskCache = new();

    /// <summary>
    /// Cache the real values. This is separate so that we can reuse the cached values while fetching new ones
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CacheEntry> _valueCache = new();

    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _refetchBeforeExpiry;
    private readonly int _maxCacheEntries;

    /// <summary>
    /// Instantiates a new <see cref="LazyRefreshCache{TKey,TValue}"/> instance.
    /// </summary>
    /// <param name="timeProvider">The timeprovider service.</param>
    /// <param name="refetchBeforeExpiry">The threshold at which to refresh cache entries <em>before</em> they expire.</param>
    /// <param name="maxCacheEntries">
    /// Maximum entries to store in cache. If evictions are required, FIFO policy will be used.
    /// To allow unlimited cache entries, set this value to zero.
    /// </param>
    public LazyRefreshCache(TimeProvider timeProvider, TimeSpan refetchBeforeExpiry, int maxCacheEntries = 0)
    {
        _refetchBeforeExpiry = refetchBeforeExpiry;
        _timeProvider = timeProvider;
        _maxCacheEntries = maxCacheEntries;
    }

    /// <summary>
    /// Retrieves a value from the cache, or creates it if not present or expired.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="valueFactory">Factory function to create the value (if not in cache).</param>
    /// <param name="lifetimeFactory">Factory function to calculate the lifetime of the cached value.</param>
    public async Task<TValue> GetOrCreate(
        TKey key,
        Func<Task<TValue>> valueFactory,
        Func<TValue, TimeSpan> lifetimeFactory
    )
    {
        var now = _timeProvider.GetUtcNow();

        if (_valueCache.TryGetValue(key, out var entry))
        {
            // If the entry is still valid (with margin), return it
            if (entry.RefreshAt > now)
            {
                return entry.Value;
            }

            // Kick off a new valueFactory task
            var cacheTask = GetTaskFromTaskCache(key, valueFactory, lifetimeFactory, now);

            // Return previous value without awaiting value if it is still valid
            if (entry.Expiry > now)
            {
                return entry.Value;
            }

            // Await the new value if the previous value has expired
            return await cacheTask;
        }

        // No previous value found, so we need to await the task from the initializer cache
        return await GetTaskFromTaskCache(key, valueFactory, lifetimeFactory, now);
    }

    /// <summary>
    /// Retrieves a value from the cache, or creates it if not present or expired.
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="valueFactory">Factory function to create the value (if not in cache).</param>
    /// <param name="lifetime">The lifetime of the cached value.</param>
    public async Task<TValue> GetOrCreate(TKey key, Func<Task<TValue>> valueFactory, TimeSpan? lifetime = default)
    {
        return await GetOrCreate(
            key: key,
            valueFactory: valueFactory,
            lifetimeFactory: _ => lifetime ?? DateTimeOffset.MaxValue - _timeProvider.GetUtcNow()
        );
    }

    private async Task<TValue> GetTaskFromTaskCache(
        TKey key,
        Func<Task<TValue>> valueFactory,
        Func<TValue, TimeSpan> lifetimeFactory,
        DateTimeOffset now
    )
    {
        return await _taskCache
            .GetOrAdd(
                key,
                _ => new Lazy<Task<TValue>>(async () =>
                {
                    var valueTask = valueFactory();
                    var value = await valueTask;
                    var lifetime = lifetimeFactory(value);
                    var expiry = now + lifetime;
                    var refreshAt = expiry - _refetchBeforeExpiry;
                    var cacheEntry = new CacheEntry(expiry, refreshAt, value);

                    // Expired items in _valueCache?
                    var expiredItems = _valueCache.Where(x => x.Value.Expiry <= now).Select(x => x.Key);
                    foreach (var expiredKey in expiredItems)
                    {
                        _valueCache.TryRemove(expiredKey, out var _);
                    }

                    // Store the new value in _valueCache
                    _valueCache.AddOrUpdate(key, cacheEntry, (_, _) => cacheEntry);

                    // Too many items in _valueCache?
                    if (_maxCacheEntries > 0 && _valueCache.Count > _maxCacheEntries)
                    {
                        var oldestEntries = GetOverflowKeys(_maxCacheEntries);
                        foreach (var oldKey in oldestEntries)
                        {
                            _valueCache.TryRemove(oldKey, out var _);
                        }
                    }

                    // Remove the Lazy Task from the _taskCache because it is no longer needed
                    // and need to be gone when the cache is refreshed
                    _taskCache.TryRemove(key, out var _);

                    return value;
                })
            )
            .Value;
    }

    /// <summary>
    /// Helper: Gets the overflowing cache entriy keys. Eg. the keys of items that should be removed because cache size is over its limit
    /// </summary>
    /// <param name="cacheSizeLimit">The cache size limit</param>
    private IEnumerable<TKey> GetOverflowKeys(int cacheSizeLimit)
    {
        return _valueCache.OrderBy(x => x.Value.Expiry).Select(x => x.Key).Take(_valueCache.Count - cacheSizeLimit);
    }
}
