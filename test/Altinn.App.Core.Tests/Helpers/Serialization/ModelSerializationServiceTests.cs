using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Serialization;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.AppModel;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Altinn.App.Core.Tests.Helpers.Serialization;

public class ModelSerializationServiceTests
{
    [XmlRoot("testModel")]
    public class TestModel
    {
        [Required(ErrorMessage = "RequiredField is required")]
        [XmlElement("requiredField")]
        public string? RequiredField { get; set; }

        [XmlElement("optionalField")]
        public string? OptionalField { get; set; }
    }

    [Fact]
    public async Task DeserializeSingleFromStream_WithMissingRequiredField_ReturnsValidationError()
    {
        // Arrange
        var appModelMock = new Mock<IAppModel>();
        appModelMock.Setup(m => m.GetModelType("TestModel")).Returns(typeof(TestModel));

        var service = new ModelSerializationService(appModelMock.Object);

        var dataType = new Altinn.Platform.Storage.Interface.Models.DataType
        {
            Id = "testDataType",
            AppLogic = new ApplicationLogic { ClassRef = "TestModel" },
        };

        // XML with missing required field
        var xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testModel>
                <optionalField>some value</optionalField>
            </testModel>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
        // Act
        var result = await service.DeserializeSingleFromStream(stream, "application/xml", dataType);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Error.Title.Should().Be("Data validation failed");
        result.Error.Detail.Should().Contain("RequiredField is required");
    }

    [Fact]
    public async Task DeserializeSingleFromStream_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var appModelMock = new Mock<IAppModel>();
        appModelMock.Setup(m => m.GetModelType("TestModel")).Returns(typeof(TestModel));

        var service = new ModelSerializationService(appModelMock.Object);

        var dataType = new Altinn.Platform.Storage.Interface.Models.DataType
        {
            Id = "testDataType",
            AppLogic = new ApplicationLogic { ClassRef = "TestModel" },
        };

        // XML with all required fields
        var xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testModel>
                <requiredField>required value</requiredField>
                <optionalField>optional value</optionalField>
            </testModel>
            """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));

        // Act
        var result = await service.DeserializeSingleFromStream(stream, "application/xml", dataType);

        // Assert
        result.Success.Should().BeTrue();
        result.Ok.Should().NotBeNull();
        var model = result.Ok.Should().BeOfType<TestModel>().Subject;
        model.RequiredField.Should().Be("required value");
        model.OptionalField.Should().Be("optional value");
    }

    [Fact]
    public async Task DeserializeSingleFromStream_WithMalformedXml_ReturnsXmlError()
    {
        // Arrange
        var appModelMock = new Mock<IAppModel>();
        appModelMock.Setup(m => m.GetModelType("TestModel")).Returns(typeof(TestModel));

        var service = new ModelSerializationService(appModelMock.Object);

        var dataType = new Altinn.Platform.Storage.Interface.Models.DataType
        {
            Id = "testDataType",
            AppLogic = new ApplicationLogic { ClassRef = "TestModel" },
        };

        // Malformed XML
        var xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testModel>
                <requiredField>required value
            </testModel>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));

        // Act
        var result = await service.DeserializeSingleFromStream(stream, "application/xml", dataType);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Status.Should().Be(StatusCodes.Status400BadRequest);
        result.Error.Title.Should().Be("Failed to deserialize XML");
    }
}
