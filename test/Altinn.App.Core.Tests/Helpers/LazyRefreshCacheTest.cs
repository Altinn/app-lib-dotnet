using Altinn.App.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Altinn.App.Core.Tests.Helpers;

public class LazyRefreshCacheTest
{
    [Fact]
    public async Task GetOrCreate_ObeysSizeLimit_ImplementsFIFO_Correctly()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        var cacheSizeLimit = 3;
        var cache = new LazyRefreshCache<int, int>(fakeTime, TimeSpan.Zero, cacheSizeLimit);

        // Act
        for (var i = 0; i < 10; i++)
        {
            await cache.GetOrCreate(i, () => Task.FromResult(i), s => TimeSpan.FromSeconds(i));
        }

        // Assert
        cache.Count.Should().Be(cacheSizeLimit);
        cache.Keys.Should().BeEquivalentTo([7, 8, 9]);
    }

    [Fact]
    public async Task GetOrCreate_RefreshesItem_BeforeExpiry_WithinThreshold()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        var refetchBeforeExpiryThreshold = TimeSpan.FromSeconds(30);
        var itemLifetime = refetchBeforeExpiryThreshold * 2;
        var cache = new LazyRefreshCache<string, CacheItem>(fakeTime, refetchBeforeExpiryThreshold);

        // Act
        var item1 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));

        fakeTime.Advance(refetchBeforeExpiryThreshold);

        var item2 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));
        var item2Expiry = cache.Values.First().Expiry;
        var item2Now = fakeTime.GetUtcNow();

        var item3 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));

        // Assert
        cache.Count.Should().Be(1);
        item1.Should().BeSameAs(item2);
        item3.Should().NotBeSameAs(item2);

        item2Expiry.Should().BeAfter(item2Now);
    }

    [Fact]
    public async Task GetOrCreate_RefreshesExpiredItems()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        var itemLifetime = TimeSpan.FromSeconds(60);
        var cache = new LazyRefreshCache<string, CacheItem>(fakeTime, TimeSpan.Zero);

        // Act
        var item1 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));
        var item2 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));

        fakeTime.Advance(itemLifetime);

        var item3 = await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));

        // Assert
        cache.Count.Should().Be(1);
        item1.Should().BeSameAs(item2);
        item3.Should().NotBeSameAs(item2);
    }

    [Fact]
    public async Task GetOrCreate_EvictsExpiredItems()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        var itemLifetime = TimeSpan.FromSeconds(60);
        var cache = new LazyRefreshCache<string, CacheItem>(fakeTime, TimeSpan.Zero);

        // Act & Assert
        await cache.GetOrCreate("a", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));
        await cache.GetOrCreate("b", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));
        cache.Count.Should().Be(2);

        fakeTime.Advance(itemLifetime);

        await cache.GetOrCreate("c", GenerateItemFactory(), GenerateLifetimeFactory(itemLifetime));
        cache.Count.Should().Be(1);
    }

    private Func<Task<CacheItem>> GenerateItemFactory(string? contents = default)
    {
        return async () => await Task.FromResult(new CacheItem(contents));
    }

    private Func<CacheItem, TimeSpan> GenerateLifetimeFactory(TimeSpan lifetime)
    {
        return _ => lifetime;
    }
}

public record CacheItem(string? Contents = default);
