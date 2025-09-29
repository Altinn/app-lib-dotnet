using Altinn.App.Core.EFormidling.Implementation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Altinn.App.Core.Tests.Eformidling.Implementation;

public class EFormidlingConfigurationProviderTests
{
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock = new();
    private readonly EFormidlingConfigurationProvider _provider;

    public EFormidlingConfigurationProviderTests()
    {
        _provider = new EFormidlingConfigurationProvider(
            _appMetadataMock.Object,
            _processReaderMock.Object,
            _hostEnvironmentMock.Object
        );
    }

    [Fact]
    public async Task GetLegacyConfiguration_ReturnsConfigFromApplicationMetadata()
    {
        // Arrange
        var applicationMetadata = new ApplicationMetadata("tdd/test")
        {
            EFormidling = new EFormidlingContract
            {
                Process = "urn:no:difi:profile:arkivmelding:administrasjon:ver1.0",
                Standard = "urn:no:difi:arkivmelding:xsd::arkivmelding",
                TypeVersion = "2.0",
                Type = "arkivmelding",
                SecurityLevel = 3,
                DPFShipmentType = "altinn3.skjema",
                DataTypes = new List<string> { "datatype1", "datatype2" },
            },
        };

        _appMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        // Act
        var result = await _provider.GetLegacyConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.Process.Should().Be("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0");
        result.Standard.Should().Be("urn:no:difi:arkivmelding:xsd::arkivmelding");
        result.TypeVersion.Should().Be("2.0");
        result.Type.Should().Be("arkivmelding");
        result.SecurityLevel.Should().Be(3);
        result.DpfShipmentType.Should().Be("altinn3.skjema");
        result.DataTypes.Should().BeEquivalentTo(new[] { "datatype1", "datatype2" });
    }

    [Fact]
    public async Task GetLegacyConfiguration_WithNullDataTypes_ReturnsConfigWithEmptyDataTypes()
    {
        // Arrange
        var applicationMetadata = new ApplicationMetadata("tdd/test")
        {
            EFormidling = new EFormidlingContract
            {
                Process = "urn:no:difi:profile:arkivmelding:administrasjon:ver1.0",
                Standard = "urn:no:difi:arkivmelding:xsd::arkivmelding",
                TypeVersion = "2.0",
                Type = "arkivmelding",
                SecurityLevel = 3,
                DataTypes = null,
            },
        };

        _appMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        // Act
        var result = await _provider.GetLegacyConfiguration();

        // Assert
        result.DataTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBpmnConfiguration_ReturnsConfigFromBpmnTask()
    {
        // Arrange
        var taskId = "Task_1";

        var taskExtension = new AltinnTaskExtension
        {
            EFormidlingConfiguration = new AltinnEFormidlingConfiguration
            {
                Process = CreateEnvironmentConfig("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0"),
                Standard = CreateEnvironmentConfig("urn:no:difi:arkivmelding:xsd::arkivmelding"),
                TypeVersion = CreateEnvironmentConfig("2.0"),
                Type = CreateEnvironmentConfig("arkivmelding"),
                SecurityLevel = CreateEnvironmentConfig("3"),
                DpfShipmentType = CreateEnvironmentConfig("altinn3.skjema"),
                Receiver = CreateEnvironmentConfig("123456789"),
            },
        };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(taskId)).Returns(taskExtension);
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");

        // Act
        var result = await _provider.GetBpmnConfiguration(taskId);

        // Assert
        result.Should().NotBeNull();
        result.Process.Should().Be("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0");
        result.Standard.Should().Be("urn:no:difi:arkivmelding:xsd::arkivmelding");
        result.TypeVersion.Should().Be("2.0");
        result.Type.Should().Be("arkivmelding");
        result.SecurityLevel.Should().Be(3);
        result.DpfShipmentType.Should().Be("altinn3.skjema");
        result.Receiver.Should().Be("123456789");
    }

    [Fact]
    public async Task GetBpmnConfiguration_NoTaskExtension_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskId = "Task_1";

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(taskId)).Returns((AltinnTaskExtension?)null);

        // Act & Assert
        var act = async () => await _provider.GetBpmnConfiguration(taskId);
        await act.Should()
            .ThrowAsync<ApplicationConfigException>()
            .WithMessage("No eFormidling configuration found in BPMN for task Task_1");
    }

    [Fact]
    public async Task GetBpmnConfiguration_NoEFormidlingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskId = "Task_1";

        var taskExtension = new AltinnTaskExtension { EFormidlingConfiguration = null };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(taskId)).Returns(taskExtension);

        // Act & Assert
        var act = async () => await _provider.GetBpmnConfiguration(taskId);
        await act.Should()
            .ThrowAsync<ApplicationConfigException>()
            .WithMessage("No eFormidling configuration found in BPMN for task Task_1");
    }

    [Fact]
    public async Task GetBpmnConfiguration_MissingRequiredConfig_ThrowsApplicationConfigException()
    {
        // Arrange
        var taskId = "Task_1";

        var taskExtension = new AltinnTaskExtension
        {
            EFormidlingConfiguration = new AltinnEFormidlingConfiguration
            {
                Process = CreateEnvironmentConfig("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0"),
                // Missing Standard, TypeVersion, Type, SecurityLevel
            },
        };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(taskId)).Returns(taskExtension);
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");

        // Act & Assert
        var act = async () => await _provider.GetBpmnConfiguration(taskId);
        await act.Should()
            .ThrowAsync<ApplicationConfigException>()
            .WithMessage("*No Standard configuration found for environment Production*");
    }

    [Fact]
    public async Task GetBpmnConfiguration_WithDataTypes_ReturnsDataTypesForEnvironment()
    {
        // Arrange
        var taskId = "Task_1";

        var eFormidlingConfig = new AltinnEFormidlingConfiguration
        {
            Process = CreateEnvironmentConfig("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0"),
            Standard = CreateEnvironmentConfig("urn:no:difi:arkivmelding:xsd::arkivmelding"),
            TypeVersion = CreateEnvironmentConfig("2.0"),
            Type = CreateEnvironmentConfig("arkivmelding"),
            SecurityLevel = CreateEnvironmentConfig("3"),
            DataTypes = new List<AltinnEFormidlingDataTypesConfig>
            {
                new()
                {
                    Environment = "Production",
                    DataTypeIds = new List<string> { "datatype1" },
                },
                new()
                {
                    Environment = "Development",
                    DataTypeIds = new List<string> { "datatype2" },
                },
            },
        };

        var taskExtension = new AltinnTaskExtension { EFormidlingConfiguration = eFormidlingConfig };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(taskId)).Returns(taskExtension);
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");

        // Act
        var result = await _provider.GetBpmnConfiguration(taskId);

        // Assert
        result.DataTypes.Should().BeEquivalentTo(new[] { "datatype1" });
    }

    [Fact]
    public void GetBpmnConfiguration_NullTaskId_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _provider.GetBpmnConfiguration(null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    // Note: Test for unknown source is no longer needed with direct method approach

    private static List<AltinnEnvironmentConfig> CreateEnvironmentConfig(string value)
    {
        return new List<AltinnEnvironmentConfig>
        {
            new() { Environment = "Production", Value = value },
        };
    }
}
