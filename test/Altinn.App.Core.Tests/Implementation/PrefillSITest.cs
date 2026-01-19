using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Dan;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests;

public class PrefillTestDataModel
{
    public TestPrefillFields? Prefill { get; set; }
}

public class PrefillDanTestDataModel
{
    public string Email { get; set; }
    public string OrganizationNumber { get; set; }
}

public class TestPrefillFields
{
    public string? EraSourceEnvironment { get; set; }
    public string? KanOppretteAarligMelding { get; set; }
    public string? ArkivSaksId { get; set; }
    public string? InnsendingSvarfrist { get; set; }
    public string? YrkesskadeforsikringPolisenummer { get; set; }
    public string? YrkesskadeforsikringNavn { get; set; }
    public string? YrkesskadeforsikringGyldigTilDato { get; set; }
}

public class PrefillSITests
{
    [Fact]
    public async Task PrefillDataModel_AssignsValuesCorrectly()
    {
        var externalPrefill = new Dictionary<string, string>
        {
            { "Prefill.EraSourceEnvironment", "prod" },
            { "Prefill.KanOppretteAarligMelding", "True" },
            { "Prefill.ArkivSaksId", "1203228" },
            { "Prefill.InnsendingSvarfrist", "2025-01-01T00:00:00.0000000" },
            { "Prefill.YrkesskadeforsikringPolisenummer", "301738.1" },
            { "Prefill.YrkesskadeforsikringNavn", "S'oderberg og Partners" },
            { "Prefill.YrkesskadeforsikringGyldigTilDato", "2023-12-31T12:00:00.000+01:00" },
        };

        var dataModel = new PrefillTestDataModel();

        var loggerMock = new Mock<ILogger<PrefillSI>>();
        var appResourcesMock = new Mock<IAppResources>();
        var authenticationContextMock = new Mock<IAuthenticationContext>();
        var services = new ServiceCollection();
        var registryClientMock = new Mock<IRegisterClient>();
        var danClientMock = new Mock<IDanClient>();
        services.AddSingleton<IRegisterClient>(registryClientMock.Object);
        await using var sp = services.BuildStrictServiceProvider();

        var prefillToTest = new PrefillSI(
            loggerMock.Object,
            appResourcesMock.Object,
            authenticationContextMock.Object,
            sp,
            danClientMock.Object
        );

        prefillToTest.PrefillDataModel(dataModel, externalPrefill, continueOnError: false);

        Assert.NotNull(dataModel.Prefill);
        Assert.Equal("prod", dataModel.Prefill.EraSourceEnvironment);
        Assert.Equal("True", dataModel.Prefill.KanOppretteAarligMelding);
        Assert.Equal("1203228", dataModel.Prefill.ArkivSaksId);
        Assert.Equal("2025-01-01T00:00:00.0000000", dataModel.Prefill.InnsendingSvarfrist);
        Assert.Equal("301738.1", dataModel.Prefill.YrkesskadeforsikringPolisenummer);
        Assert.Equal("S'oderberg og Partners", dataModel.Prefill.YrkesskadeforsikringNavn);
        Assert.Equal("2023-12-31T12:00:00.000+01:00", dataModel.Prefill.YrkesskadeforsikringGyldigTilDato);
    }

    [Fact]
    public async Task PrefillDataModel_Should_Fill_With_Data_From_Dan()
    {
        // Arrange
        var dataModel = new PrefillDanTestDataModel();

        var loggerMock = new Mock<ILogger<PrefillSI>>();
        var appResourcesMock = new Mock<IAppResources>();
        var authenticationContextMock = new Mock<IAuthenticationContext>();
        var services = new ServiceCollection();
        var registryClientMock = new Mock<IRegisterClient>();
        var danClientMock = new Mock<IDanClient>();
        services.AddSingleton<IRegisterClient>(registryClientMock.Object);
        await using var sp = services.BuildStrictServiceProvider();

        var prefillToTest = new PrefillSI(
            loggerMock.Object,
            appResourcesMock.Object,
            authenticationContextMock.Object,
            sp,
            danClientMock.Object
        );

        var modelName = "model";
        var partyId = "1234";

        // danData should match the data in the GetJsonConfig method
        var danData = new Dictionary<string, string>
        {
            { "BusinessAddressCity", "Email" },
            { "SectorCode", "OrganizationNumber" },
        };

        var party = new Party() { PartyId = 1234, SSN = "12341234" };

        appResourcesMock.Setup(ar => ar.GetPrefillJson(It.IsAny<string>())).Returns(GetJsonConfig());
        registryClientMock
            .Setup(m => m.GetPartyUnchecked(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(party);
        danClientMock
            .Setup(m => m.GetDataset(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(danData);

        //Act
        await prefillToTest.PrefillDataModel(partyId, modelName, dataModel);

        //Assert
        danClientMock.Verify(
            m => m.GetDataset(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce
        );
    }

    private string GetJsonConfig()
    {
        return "{\n  \"$schema\" : \"https://altinncdn.no/schemas/json/prefill/prefill.schema.v1.json\",\n  \"allowOverwrite\" : true,\n  \"DAN\" : {\n    \"datasets\" : [ {\n      \"name\" : \"UnitBasicInformation\",\n      \"mappings\" : [ {\n        \"BusinessAddressCity\" : \"Email\"\n      }, {\n        \"SectorCode\" : \"OrganizationNumber\"\n      } ]\n    } ]\n  }\n}";
    }
}
