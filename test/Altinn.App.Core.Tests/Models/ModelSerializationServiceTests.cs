using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.AppModel;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;

namespace Altinn.App.Core.Tests.Models;

public class ModelSerializationServiceTests
{
    private static readonly string _testClassRef = typeof(TestDataModel).AssemblyQualifiedName!;
    private static readonly string _otherClassRef = typeof(SomeOtherType).AssemblyQualifiedName!;

    private readonly ModelSerializationService _sut;

    private readonly Mock<IAppModel> _appModelMock = new Mock<IAppModel>();

    public ModelSerializationServiceTests()
    {
        _appModelMock.Setup(x => x.GetModelType(It.Is<string>(s => s == _testClassRef))).Returns(typeof(TestDataModel));
        _appModelMock
            .Setup(x => x.GetModelType(It.Is<string>(s => s == _otherClassRef)))
            .Returns(typeof(SomeOtherType));

        _sut = new ModelSerializationService(_appModelMock.Object);
    }

    [Theory]
    [InlineData("application/json", "application/json")]
    [InlineData("application/xml", "application/xml")]
    [InlineData(null, "application/xml")]
    public void SerializeToStorage_ReturnsExpectedContentType(string? contentType, string expectedOutputType)
    {
        List<string> allowedContentTypes = contentType != null ? [contentType] : [];

        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };

        var dataType = CreateDataType(allowedContentTypes);

        // Act
        var (_, outputContentType) = _sut.SerializeToStorage(testObject, dataType);

        // Assert
        outputContentType.Should().Be(expectedOutputType);
    }

    [Fact]
    public void SerializeToStorage_SerializesJson()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };

        var dataType = CreateDataType(["application/json"]);

        // Act
        var (data, _) = _sut.SerializeToStorage(testObject, dataType);

        // Assert
        var json = System.Text.Encoding.UTF8.GetString(data.Span);

        json.Should().Be("{\"name\":\"Test\",\"value\":42}");
    }

    [Fact]
    public void SerializeToStorage_SerializesXml()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };

        var dataType = CreateDataType(["application/xml"]);

        // Act
        var (data, _) = _sut.SerializeToStorage(testObject, dataType);

        // Assert
        var xml = System.Text.Encoding.UTF8.GetString(data.Span);

        xml.Should()
            .Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestDataModel xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><Name>Test</Name><Value>42</Value></TestDataModel>"
            );
    }

    [Fact]
    public void SerializeToStorage_ThrowsOnUnsupportedContentType()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };

        var dataType = CreateDataType(["application/unsupported"]);

        // Act
        var act = () => _sut.SerializeToStorage(testObject, dataType);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SerializeToStorage_ThrowsOnMismatchingModelType()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };

        var mismatchingDataType = new DataType()
        {
            Id = "mismatching",
            AppLogic = new ApplicationLogic() { ClassRef = _otherClassRef },
            AllowedContentTypes = ["application/json"],
        };

        // Act
        var act = () => _sut.SerializeToStorage(testObject, mismatchingDataType);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeFromStorage_ThrowsOnMismatchingModelType()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };
        var data = _sut.SerializeToJson(testObject);

        var mismatchingDataType = new DataType()
        {
            Id = "mismatching",
            AppLogic = new ApplicationLogic() { ClassRef = _otherClassRef },
        };

        // Act
        var act = () => _sut.DeserializeFromStorage(data.Span, mismatchingDataType);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeFromStorage_DeserializesJson()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };
        var data = _sut.SerializeToJson(testObject);

        var dataType = CreateDataType(["application/json"]);

        // Act
        var result = _sut.DeserializeFromStorage(data.Span, dataType, "application/json");

        // Assert
        result.Should().BeOfType<TestDataModel>();
        var model = (TestDataModel)result;
        model.Name.Should().Be("Test");
        model.Value.Should().Be(42);
    }

    [Fact]
    public void DeserializeFromStorage_DeserializesXml()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };
        var data = _sut.SerializeToXml(testObject);

        var dataType = CreateDataType(["application/xml"]);

        // Act
        var result = _sut.DeserializeFromStorage(data.Span, dataType, "application/xml");

        // Assert
        result.Should().BeOfType<TestDataModel>();
        var model = (TestDataModel)result;
        model.Name.Should().Be("Test");
        model.Value.Should().Be(42);
    }

    [Fact]
    public void DeserializeFromStorage_WithDefaultContentType_DeserializesXml()
    {
        // Arrange
        var testObject = new TestDataModel { Name = "Test", Value = 42 };
        var data = _sut.SerializeToXml(testObject);

        var dataType = CreateDataType(["application/xml"]);

        // Act
        var result = _sut.DeserializeFromStorage(data.Span, dataType);

        // Assert
        result.Should().BeOfType<TestDataModel>();
        var model = (TestDataModel)result;
        model.Name.Should().Be("Test");
        model.Value.Should().Be(42);
    }

    private static DataType CreateDataType(List<string> allowedContentTypes)
    {
        return new DataType()
        {
            AppLogic = new ApplicationLogic() { ClassRef = _testClassRef },
            AllowedContentTypes = allowedContentTypes,
        };
    }

    public record SomeOtherType { }

    public record TestDataModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
