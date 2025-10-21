using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
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

    [Fact]
    public async Task GetArchiveDocumentMetadata_NoMetadataConfiguration_ReturnsNull()
    {
        // Arrange
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Documents = new FiksArkivDocumentSettings
            {
                PrimaryDocument = new FiksArkivDataTypeSettings { DataType = "test", Filename = "test.xml" },
            },
            Metadata = null,
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        var instance = new Instance();

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetArchiveDocumentMetadata(instance);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null, null, null, null, null)]
    [InlineData("the-system-id", null, null, null, null)]
    [InlineData(null, "the-rule-id", null, null, null)]
    [InlineData(null, null, "the-case-file-id", null, null)]
    [InlineData(null, null, null, "the-case-file-title", null)]
    [InlineData(null, null, null, null, "the-journal-entry-title")]
    public async Task GetArchiveDocumentMetadata_ResolvesCorrectly_StaticValues(
        string? systemId,
        string? ruleId,
        string? caseFileId,
        string? caseFileTitle,
        string? journalEntryTitle
    )
    {
        // Arrange
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Metadata = new FiksArkivMetadataSettings
            {
                SystemId = systemId is null ? null : new FiksArkivBindableValue<string> { Value = systemId },
                RuleId = ruleId is null ? null : new FiksArkivBindableValue<string> { Value = ruleId },
                CaseFileId = caseFileId is null ? null : new FiksArkivBindableValue<string> { Value = caseFileId },
                CaseFileTitle = caseFileTitle is null
                    ? null
                    : new FiksArkivBindableValue<string> { Value = caseFileTitle },
                JournalEntryTitle = journalEntryTitle is null
                    ? null
                    : new FiksArkivBindableValue<string> { Value = journalEntryTitle },
            },
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetArchiveDocumentMetadata(Mock.Of<Instance>());

        // Assert
        Assert.Equal(systemId, result?.SystemId);
        Assert.Equal(ruleId, result?.RuleId);
        Assert.Equal(caseFileId, result?.CaseFileId);
        Assert.Equal(caseFileTitle, result?.CaseFileTitle);
        Assert.Equal(journalEntryTitle, result?.JournalEntryTitle);
    }

    [Fact]
    public async Task GetArchiveDocumentMetadata_ResolvesCorrectly_BoundValues()
    {
        // Arrange
        var model = new
        {
            systemId = "bound-system-id",
            ruleId = "bound-rule-id",
            caseFileId = "bound-case-file-id",
            titles = new { caseFile = "bound-case-file-title", journalEntry = "bound-journal-entry-title" },
        };
        var modelDataType = new DataType { Id = "model" };
        var modelDataElement = new DataElement { Id = Guid.NewGuid().ToString(), DataType = modelDataType.Id };
        var instance = new Instance { Data = [modelDataElement] };

        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Metadata = new FiksArkivMetadataSettings
            {
                SystemId = new FiksArkivBindableValue<string>
                {
                    DataModelBinding = new FiksArkivDataModelBinding
                    {
                        DataType = modelDataType.Id,
                        Field = "systemId",
                    },
                },
                RuleId = new FiksArkivBindableValue<string>
                {
                    DataModelBinding = new FiksArkivDataModelBinding { DataType = modelDataType.Id, Field = "ruleId" },
                },
                CaseFileId = new FiksArkivBindableValue<string>
                {
                    DataModelBinding = new FiksArkivDataModelBinding
                    {
                        DataType = modelDataType.Id,
                        Field = "caseFileId",
                    },
                },
                CaseFileTitle = new FiksArkivBindableValue<string>
                {
                    DataModelBinding = new FiksArkivDataModelBinding
                    {
                        DataType = modelDataType.Id,
                        Field = "titles.caseFile",
                    },
                },
                JournalEntryTitle = new FiksArkivBindableValue<string>
                {
                    DataModelBinding = new FiksArkivDataModelBinding
                    {
                        DataType = modelDataType.Id,
                        Field = "titles.journalEntry",
                    },
                },
            },
        };

        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        var instanceDataAccessorMock = new Mock<IInstanceDataAccessor>();
        instanceDataAccessorMock.Setup(x => x.Instance).Returns(instance);
        instanceDataAccessorMock.Setup(x => x.DataTypes).Returns([modelDataType]);
        instanceDataAccessorMock
            .Setup(x => x.GetDataElement(It.IsAny<DataElementIdentifier>()))
            .Returns(modelDataElement);
        instanceDataAccessorMock
            .Setup(x => x.GetFormDataWrapper(It.IsAny<DataElementIdentifier>()))
            .ReturnsAsync(FormDataWrapperFactory.Create(model));

        fixture
            .LayoutStateInitializerMock.Setup(x =>
                x.Init(It.IsAny<IInstanceDataAccessor>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync(
                new LayoutEvaluatorState(
                    instanceDataAccessorMock.Object,
                    null,
                    fixture.TranslationServiceMock.Object,
                    new FrontEndSettings()
                )
            );

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetArchiveDocumentMetadata(instance);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(model.systemId, result.SystemId);
        Assert.Equal(model.ruleId, result.RuleId);
        Assert.Equal(model.caseFileId, result.CaseFileId);
        Assert.Equal(model.titles.caseFile, result.CaseFileTitle);
        Assert.Equal(model.titles.journalEntry, result.JournalEntryTitle);
    }
}
