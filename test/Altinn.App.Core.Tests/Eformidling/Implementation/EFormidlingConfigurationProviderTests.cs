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

public class EFormidlingIeFormidlingLegacyConfigurationProviderTests
{
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly EFormidlingIeFormidlingLegacyConfigurationProvider _provider;

    public EFormidlingIeFormidlingLegacyConfigurationProviderTests()
    {
        _provider = new EFormidlingIeFormidlingLegacyConfigurationProvider(_appMetadataMock.Object);
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
        ValidAltinnEFormidlingConfiguration result = await _provider.GetLegacyConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.Process.Should().Be("urn:no:difi:profile:arkivmelding:administrasjon:ver1.0");
        result.Standard.Should().Be("urn:no:difi:arkivmelding:xsd::arkivmelding");
        result.TypeVersion.Should().Be("2.0");
        result.Type.Should().Be("arkivmelding");
        result.SecurityLevel.Should().Be(3);
        result.DpfShipmentType.Should().Be("altinn3.skjema");
        result.DataTypes.Should().BeEquivalentTo("datatype1", "datatype2");
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
        ValidAltinnEFormidlingConfiguration result = await _provider.GetLegacyConfiguration();

        // Assert
        result.DataTypes.Should().BeEmpty();
    }
}
