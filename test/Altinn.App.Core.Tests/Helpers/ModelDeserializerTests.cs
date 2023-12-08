using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Altinn.App.Core.Helpers.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Helpers;

public class ModelDeserializerTests
{
    [Fact]
    public async Task TestDeserializeJson()
    {
        // Arrange
        string json = @"{""test"":""test""}";
        var logger = new Mock<ILogger>().Object;

        // Act
        var deserializer = new ModelDeserializer(logger, typeof(Melding));
        var result = await deserializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), "application/json");

        // Assert
        result.HasError.Should().BeFalse();
        result.Model.Should().BeOfType<Melding>().Which.Test.Should().Be("test");
    }

    [Fact]
    public async Task TestDeserializeXml()
    {
        // Arrange
        string json = "<melding><test>test</test></melding>";
        var logger = new Mock<ILogger>().Object;

        // Act
        var deserializer = new ModelDeserializer(logger, typeof(Melding));
        var result = await deserializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), "application/xml");

        // Assert
        result.HasError.Should().BeFalse(result.Error);
        result.Model.Should().BeOfType<Melding>().Which.Test.Should().Be("test");
    }

    [Fact]
    public async Task TestDeserializeInvalidXml()
    {
        // Arrange
        string json = "<melding><testFail>test</estFail></melding>";
        var logger = new Mock<ILogger>().Object;

        // Act
        var deserializer = new ModelDeserializer(logger, typeof(Melding));
        var result = await deserializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), "application/xml");

        // Assert
        result.HasError.Should().BeTrue();
        result.Error.Should().Contain("The 'testFail' start tag on line 1 position 11 does not match the end tag of 'estFail'. Line 1, position 26.");
    }

    [XmlRoot("melding")]
    public class Melding
    {
        [JsonPropertyName("test")]
        [XmlElement("test")]
        public string Test { get; set; }
    }
}