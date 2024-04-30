using Altinn.App.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_WithNullCollection_ShouldReturnTrue()
    {
        // Arrange
        IEnumerable<int> nullCollection = null!;

        // Act
        bool result = nullCollection.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue("because the collection is null");
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyCollection_ShouldReturnTrue()
    {
        // Arrange
        IEnumerable<int> emptyCollection = [];

        // Act
        bool result = emptyCollection.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue("because the collection is empty");
    }

    [Fact]
    public void IsNullOrEmpty_WithNonEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        IEnumerable<int> nonEmptyCollection = [1, 2, 3];

        // Act
        bool result = nonEmptyCollection.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse("because the collection contains elements");
    }
}
