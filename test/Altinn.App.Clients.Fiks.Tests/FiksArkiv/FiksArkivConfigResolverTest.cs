using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Moq;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivConfigResolverTest
{
    [Fact]
    public async Task PrimaryDocumentSettings_ResolvesCorrectly()
    {
        // Arrange
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Documents = new FiksArkivDocumentSettings
            {
                PrimaryDocument = new FiksArkivDataTypeSettings { DataType = "test", Filename = "thefilename.xml" },
            },
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        // Act
        var result = fixture.FiksArkivConfigResolver.PrimaryDocumentSettings;

        // Assert
        Assert.Equal("test", result.DataType);
        Assert.Equal("thefilename.xml", result.Filename);
    }

    [Fact]
    public async Task PrimaryDocumentSettings_ThrowsForMissingValues()
    {
        // Arrange
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Documents = new FiksArkivDocumentSettings { PrimaryDocument = null! },
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        // Act
        var ex = Record.Exception(() => fixture.FiksArkivConfigResolver.PrimaryDocumentSettings);

        // Assert
        Assert.IsType<FiksArkivConfigurationException>(ex);
    }

    [Theory]
    [InlineData(new[] { "attachment-type-1", "attachment-type-2" }, 2)]
    [InlineData(null, 0)]
    public async Task AttachmentSettings_ResolvesCorrectly(IReadOnlyList<string>? attachments, int expectedCount)
    {
        // Arrange
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Documents = new FiksArkivDocumentSettings
            {
                PrimaryDocument = null!,
                Attachments = attachments
                    ?.Select(x => new FiksArkivDataTypeSettings { DataType = x, Filename = $"{x}.ext" })
                    .ToList(),
            },
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        // Act
        var result = fixture.FiksArkivConfigResolver.AttachmentSettings;

        // Assert
        Assert.Equal(expectedCount, result.Count);
        for (int i = 0; i < expectedCount; i++)
        {
            Assert.Equal(attachments![i], result[i].DataType);
            Assert.Equal($"{attachments[i]}.ext", result[i].Filename);
        }
    }

    [Theory]
    [InlineData("ttd/test-app", null, null, "test-app")]
    [InlineData("ttd/test-app", "Custom name", null, "Custom name")]
    [InlineData("ttd/test-app", null, "Custom title", "Custom title")]
    [InlineData("ttd/test-app", "Custom name", "Custom title", "Custom name")]
    public async Task GetApplicationTitle_ResolvesCorrectly(
        string appId,
        string? appName,
        string? appTitle,
        string expectedResult
    )
    {
        // Arrange
        await using var fixture = TestFixture.Create(services =>
            services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings")
        );

        fixture
            .AppMetadataMock.Setup(x => x.GetApplicationMetadata())
            .ReturnsAsync(
                new ApplicationMetadata(appId)
                {
                    Title = new Dictionary<string, string?> { [LanguageConst.Nb] = appTitle },
                }
            );

        fixture
            .TranslationServiceMock.Setup(x => x.TranslateTextKey("appName", LanguageConst.Nb, null))
            .ReturnsAsync(appName);

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetApplicationTitle();

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
