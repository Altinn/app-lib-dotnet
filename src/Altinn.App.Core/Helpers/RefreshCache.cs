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
    public record CacheEntry(DateTimeOffset Expiry, TValue Value);

    /// <summary>
    /// The collection that holds all cached items
    /// </summary>
    private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();

    private const int NumLocks = 32;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxCacheEntries;
    private readonly SemaphoreSlim[] _locks;

    /// <summary>
    /// Instantiates a new <see cref="RefreshCache{TKey,TValue}"/> instance.
    /// </summary>
    /// <param name="timeProvider">The timeprovider service.</param>
    /// <param name="maxCacheEntries">
    /// Maximum entries to store in cache. If evictions are required, FIFO policy will be used.
    /// To allow unlimited cache entries, set this value to zero.
    /// </param>
    public RefreshCache(TimeProvider timeProvider, int maxCacheEntries = 0)
    {
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
        var now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(key, out var entry) && entry.Expiry > now)
        {
            postProcessCallback?.Invoke(entry.Value, CacheResultType.Cached);
            return entry.Value;
        }

        return await Create(key, valueFactory, lifetimeFactory, now);
    }

    private async Task<TValue> Create(
        TKey key,
        Func<Task<TValue>> valueFactory,
        Func<TValue, TimeSpan> lifetimeFactory,
        DateTimeOffset now,
        Action<TValue?, CacheResultType>? postProcessCallback = default
    )
    {
        var lockNo = (key.GetHashCode() & 0x7fffffff) % NumLocks;
        var semaphore = _locks[lockNo];
        await semaphore.WaitAsync();

        try
        {
            // Verify that the cached was not populated while waiting on the semaphore
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > now)
            {
                postProcessCallback?.Invoke(entry.Value, CacheResultType.Cached);
                return entry.Value;
            }

            // execute the factory and create a new item
            var value = await valueFactory();
            var lifetime = lifetimeFactory(value);
            var expiry = now + lifetime;
            entry = new CacheEntry(expiry, value);

            // Remove expired items
            foreach (var expiredItem in _cache.Where(x => x.Value.Expiry <= now))
            {
                _cache.TryRemove(expiredItem);
            }

            // Store the new value
            _cache.AddOrUpdate(key, entry, (_, _) => entry);

            // Too many items? Prune the oldest
            if (_maxCacheEntries > 0 && _cache.Count > _maxCacheEntries)
            {
                var oldestItem = _cache.MinBy(x => x.Value.Expiry);
                _cache.TryRemove(oldestItem);
            }

            postProcessCallback?.Invoke(value, CacheResultType.New);
            return value;
        }
        finally
        {
            semaphore.Release();
        }
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
    /// The item was not found in the cache (or it was expired).
    /// A new item was generated via factory method.
    /// </summary>
    New,
}
