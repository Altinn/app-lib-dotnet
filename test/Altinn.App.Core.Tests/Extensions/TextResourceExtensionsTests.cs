using Altinn.App.Core.Extensions;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Tests.Extensions;

public class TextResourceExtensionsTests
{
    [Fact]
    public void GetTextResource_ReturnsCorrectTextResource()
    {
        // Arrange
        string textResourceKey = "TestKey";
        string expected = "Test text resource";

        TextResource textResource = new()
        {
            Resources = [new TextResourceElement { Id = textResourceKey, Value = expected }],
        };

        // Act
        string? result = textResource.GetText(textResourceKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetTextResource_ReturnsNullWhenKeyIsNull()
    {
        // Arrange
        string textResourceKey = "TestKey";
        string expected = "Test text resource";

        TextResource textResource = new()
        {
            Resources = [new TextResourceElement { Id = textResourceKey, Value = expected }],
        };

        // Act
        string? result = textResource.GetText(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetFirstMatchingText_SingleMatch_ReturnsCorrectTextResource()
    {
        // Arrange
        string textResourceKey = "TestKey";
        string expected = "Test text resource";

        TextResource textResource = new()
        {
            Resources = [new TextResourceElement { Id = textResourceKey, Value = expected }],
        };

        // Act
        string? result = textResource.GetFirstMatchingText("NonExistingKey", textResourceKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFirstMatchingText_MultipleMatches_ReturnsFirstMatch()
    {
        // Arrange
        string textResourceKey = "TestKey";
        string anotherTextResourceKey = "AnotherTestKey";
        string expected = "Test text resource";

        TextResource textResource = new()
        {
            Resources =
            [
                new TextResourceElement { Id = anotherTextResourceKey, Value = "Another test text resource" },
                new TextResourceElement { Id = textResourceKey, Value = expected },
            ],
        };

        // Act
        string? result = textResource.GetFirstMatchingText("NonExistingKey", textResourceKey, anotherTextResourceKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }
}
