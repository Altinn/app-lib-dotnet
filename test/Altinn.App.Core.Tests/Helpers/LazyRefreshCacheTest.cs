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

    // TODO: More testing
}
