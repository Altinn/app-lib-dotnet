using Altinn.App.Core.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Altinn.App.Core.Tests.Helpers;

public class LazyRefreshCacheTest
{
    private readonly FakeTimeProvider _fakeTime = new();

    [Fact]
    public async Task GetOrCreate_ObeysSizeLimit_ImplementsFIFO_Correctly()
    {
        // Arrange
        var cacheSizeLimit = 3;
        var cache = new LazyRefreshCache<int, int>(_fakeTime, TimeSpan.Zero, cacheSizeLimit);

        // Act
        for (var i = 0; i < 10; i++)
        {
            await cache.GetOrCreate(i, () => Task.FromResult(i), _ => TimeSpan.FromSeconds(i));
        }

        // Assert
        cache.Count.Should().Be(cacheSizeLimit);
        cache.Keys.Should().BeEquivalentTo([7, 8, 9]);
    }

    [Fact]
    public async Task GetOrCreate_RefreshesItem_BeforeExpiry_WithinThreshold()
    {
        // Arrange
        var refetchBeforeExpiryThreshold = TimeSpan.FromSeconds(30);
        var itemLifetime = refetchBeforeExpiryThreshold * 2;
        var cache = new LazyRefreshCache<string, CacheItem>(_fakeTime, refetchBeforeExpiryThreshold);

        // Act
        var item1 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);

        _fakeTime.Advance(refetchBeforeExpiryThreshold);

        var item2 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);
        var item2Expiry = cache.Values.First().Expiry;
        var item2Now = _fakeTime.GetUtcNow();

        var item3 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);

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
        var itemLifetime = TimeSpan.FromSeconds(60);
        var cache = new LazyRefreshCache<string, CacheItem>(_fakeTime, TimeSpan.Zero);

        // Act
        var item1 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);
        var item2 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);

        _fakeTime.Advance(itemLifetime);

        var item3 = await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);

        // Assert
        cache.Count.Should().Be(1);
        item1.Should().BeSameAs(item2);
        item3.Should().NotBeSameAs(item2);
    }

    [Fact]
    public async Task GetOrCreate_EvictsExpiredItems()
    {
        // Arrange
        var itemLifetime = TimeSpan.FromSeconds(60);
        var cache = new LazyRefreshCache<string, CacheItem>(_fakeTime, TimeSpan.Zero);

        // Act & Assert
        await cache.GetOrCreate("a", GenerateItemFactory(), itemLifetime);
        await cache.GetOrCreate("b", GenerateItemFactory(), itemLifetime);
        cache.Count.Should().Be(2);

        _fakeTime.Advance(itemLifetime);

        await cache.GetOrCreate("c", GenerateItemFactory(), itemLifetime);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreate_OnlyGeneratesOneItem_DuringStampede()
    {
        for (int i = 0; i < 100; i++)
        {
            FakeTimeProvider fakeTime = new();

            // Arrange
            var slowness = TimeSpan.FromSeconds(10);
            var mockSlowItem = new Mock<ISlowItem>();
            mockSlowItem.Setup(x => x.Delay()).Returns(Task.Delay(slowness, fakeTime));
            var value = mockSlowItem.Object;
            var cache = new LazyRefreshCache<string, CacheItem>(fakeTime, TimeSpan.Zero);

            long spinLock = 0;

            // Act
            var tasks = Enumerable
                .Range(0, 10)
                .Select(_ =>
                    Task.Run(() =>
                    {
                        while (Interlocked.Read(ref spinLock) == 0)
                        {
                            // Spin
                        }
                        return cache.GetOrCreate("a", GenerateSlowItemFactory(value), TimeSpan.FromSeconds(60));
                    })
                )
                .ToArray();

            await Task.Delay(10);
            Interlocked.Exchange(ref spinLock, 1);

            fakeTime.Advance(slowness);

            var results = await Task.WhenAll(tasks);

            // Assert
            var firstResult = results.First();
            results.All(x => ReferenceEquals(x, firstResult)).Should().BeTrue($"Failed during {i}");
            mockSlowItem.Verify(x => x.Delay(), Times.Once);
        }
    }

    [Fact]
    public async Task GetOrCreate_DefaultLifetime_IsForever()
    {
        // Arrange
        var cache = new LazyRefreshCache<string, CacheItem>(_fakeTime, TimeSpan.Zero);

        // Act
        var item1 = await cache.GetOrCreate("a", GenerateItemFactory("heya"));

        _fakeTime.Advance(TimeSpan.FromDays(365 ^ 2));

        var item2 = await cache.GetOrCreate("a", GenerateItemFactory("buddy"));

        // Assert
        item1.Should().BeSameAs(item2);
        cache.Values.Count.Should().Be(1);
        cache.Values.Select(x => x.Value.Contents).First().Should().Be("heya");
    }

    private static Func<Task<CacheItem>> GenerateItemFactory(string? contents = default)
    {
        return async () => await Task.FromResult(new CacheItem(contents));
    }

    private static Func<Task<CacheItem>> GenerateSlowItemFactory(ISlowItem slowItem, string? contents = default)
    {
        return async () =>
        {
            await slowItem.Delay();
            return new CacheItem(contents);
        };
    }
}

public record CacheItem(string? Contents = default);

public interface ISlowItem
{
    public Task Delay();
}
