using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.Data;

/// <summary>
/// Simple wrapper around a Dictionary using Lazy to ensure that the valueFactory is only called once
/// </summary>
internal sealed class DataElementCache<T>
{
    private readonly Dictionary<DataElementIdentifier, Lazy<Task<T>>> _cache = [];

    public async Task<T> GetOrCreate(DataElementIdentifier key, Func<Task<T>> valueFactory)
    {
        Lazy<Task<T>>? lazyTask;
        lock (_cache)
        {
            if (!_cache.TryGetValue(key, out lazyTask))
            {
                lazyTask = new Lazy<Task<T>>(valueFactory);
                _cache.Add(key, lazyTask);
            }
        }
        return await lazyTask.Value.ConfigureAwait(false);
    }

    public void Set(DataElementIdentifier key, T data)
    {
        lock (_cache)
        {
            _cache[key] = new Lazy<Task<T>>(Task.FromResult(data));
        }
    }

    public bool TryGetCachedValue(DataElementIdentifier identifier, [NotNullWhen(true)] out T? value)
    {
        lock (_cache)
        {
            if (
                _cache.TryGetValue(identifier, out var lazyTask)
                && lazyTask is { IsValueCreated: true, Value.IsCompletedSuccessfully: true }
            )
            {
                value = lazyTask.Value.Result ?? throw new InvalidOperationException("Value in cache is null");
                return true;
            }
        }
        value = default;
        return false;
    }

    public async IAsyncEnumerable<(DataElementIdentifier, T)> GetCachedEntries()
    {
        List<Task<(DataElementIdentifier, T)>> entries;
        lock (_cache)
        {
            entries = _cache.Select(async kv => (kv.Key, await kv.Value.Value.ConfigureAwait(false))).ToList();
        }

        // TODO: Use WhenEach when targeting .NET 9 or greater
        while (entries.Count > 0)
        {
            var entry = await Task.WhenAny(entries);
            entries.Remove(entry);
            yield return (entry.Result.Item1, entry.Result.Item2);
        }
    }
}
