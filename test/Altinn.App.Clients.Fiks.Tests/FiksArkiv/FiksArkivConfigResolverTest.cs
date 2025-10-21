using System.Text;
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
using Microsoft.Extensions.DependencyInjection;
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
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv(),
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
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv(),
            useDefaultFiksArkivSettings: false
        );

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetArchiveDocumentMetadata(Mock.Of<Instance>());

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
                SystemId = systemId is null ? null : TestHelpers.BindableValueFactory(systemId),
                RuleId = ruleId is null ? null : TestHelpers.BindableValueFactory(ruleId),
                CaseFileId = caseFileId is null ? null : TestHelpers.BindableValueFactory(caseFileId),
                CaseFileTitle = caseFileTitle is null ? null : TestHelpers.BindableValueFactory(caseFileTitle),
                JournalEntryTitle = journalEntryTitle is null
                    ? null
                    : TestHelpers.BindableValueFactory(journalEntryTitle),
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
                SystemId = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "systemId"),
                RuleId = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "ruleId"),
                CaseFileId = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "caseFileId"),
                CaseFileTitle = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "titles.caseFile"),
                JournalEntryTitle = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "titles.journalEntry"),
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

    [Fact]
    public async Task GetRecipient_ThrowsOnMissingConfig()
    {
        // Arrange
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv(),
            useDefaultFiksArkivSettings: false
        );

        // Act
        var ex = await Record.ExceptionAsync(() => fixture.FiksArkivConfigResolver.GetRecipient(Mock.Of<Instance>()));

        // Assert
        Assert.IsType<FiksArkivConfigurationException>(ex);
        Assert.Contains("must be configured", ex.Message);
    }

    [Theory]
    [InlineData("7fa66c3f-61f2-43b0-8a53-2283c12e1437", "abc123", "the-name", "123456789")]
    [InlineData("9642411c-bb5a-4775-8e89-2510f4d3ad1d", "321cba", "eman-eht", null)]
    public async Task GetRecipient_ResolvesCorrectly_StaticValues(
        string accountId,
        string identifier,
        string name,
        string? orgNumber
    )
    {
        // Arrange
        var account = Guid.Parse(accountId);
        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Recipient = new FiksArkivRecipientSettings
            {
                FiksAccount = TestHelpers.BindableValueFactory<Guid?>(account),
                Identifier = TestHelpers.BindableValueFactory(identifier),
                Name = TestHelpers.BindableValueFactory(name),
                OrganizationNumber = orgNumber is null ? null : TestHelpers.BindableValueFactory(orgNumber),
            },
        };
        await using var fixture = TestFixture.Create(
            services => services.AddFiksArkiv().WithFiksArkivConfig("CustomFiksArkivSettings"),
            [("CustomFiksArkivSettings", fiksArkivSettingsOverride)],
            useDefaultFiksArkivSettings: false
        );

        // Act
        var result = await fixture.FiksArkivConfigResolver.GetRecipient(Mock.Of<Instance>());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account, result.AccountId);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(name, result.Name);
        Assert.Equal(orgNumber, result.OrgNumber);
    }

    [Fact]
    public async Task GetRecipient_ResolvesCorrectly_BoundValues()
    {
        // Arrange
        var account = Guid.NewGuid();
        var model = new
        {
            recipient = new
            {
                accountId = account.ToString(),
                identifier = "fancy-identifier-123",
                name = "The name of the recipient",
                orgNumber = "001122334455",
            },
        };
        var modelDataType = new DataType { Id = "model" };
        var modelDataElement = new DataElement { Id = Guid.NewGuid().ToString(), DataType = modelDataType.Id };
        var instance = new Instance { Data = [modelDataElement] };

        var fiksArkivSettingsOverride = new FiksArkivSettings
        {
            Recipient = new FiksArkivRecipientSettings
            {
                FiksAccount = TestHelpers.BindableValueFactory<Guid?>(modelDataType.Id, "recipient.accountId"),
                Identifier = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "recipient.identifier"),
                Name = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "recipient.name"),
                OrganizationNumber = TestHelpers.BindableValueFactory<string>(modelDataType.Id, "recipient.orgNumber"),
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
        var result = await fixture.FiksArkivConfigResolver.GetRecipient(instance);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account, result.AccountId);
        Assert.Equal(model.recipient.identifier, result.Identifier);
        Assert.Equal(model.recipient.name, result.Name);
        Assert.Equal(model.recipient.orgNumber, result.OrgNumber);
    }

    [Fact]
    public async Task GetCorrelationId_ReturnsInstanceUrl()
    {
        // Arrange
        var appMetadata = new ApplicationMetadata("ttd/test-app");
        var instance = new Instance { Id = "12345/b66a17cd-5155-4ec8-a0b5-397c5e26880e", AppId = appMetadata.Id };
        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.Configure<GeneralSettings>(options =>
            {
                options.HostName = "the-hostname";
                options.ExternalAppBaseUrl = "https://{org}.apps.{hostName}/{org}/{app}/";
            });
        });
        fixture.AppMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(appMetadata);

        // Act
        var result = fixture.FiksArkivConfigResolver.GetCorrelationId(instance);

        // Assert
        Assert.Equal(
            "https://ttd.apps.the-hostname/ttd/test-app/instances/12345/b66a17cd-5155-4ec8-a0b5-397c5e26880e",
            result
        );
    }

    [Theory]
    [InlineData("7fa66c3f-61f2-43b0-8a53-2283c12e1437", "abc123", "the-name", "123456789")]
    [InlineData("9642411c-bb5a-4775-8e89-2510f4d3ad1d", "321cba", "eman-eht", null)]
    public async Task GetRecipientParty_ReturnsExpectedValue(
        Guid accountId,
        string identifier,
        string name,
        string? orgNumber
    )
    {
        // Arrange
        var appMetadata = new ApplicationMetadata("ttd/test-app");
        var instance = new Instance { Id = "12345/b66a17cd-5155-4ec8-a0b5-397c5e26880e", AppId = appMetadata.Id };
        var recipient = new FiksArkivRecipient(accountId, identifier, name, orgNumber);
        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.Configure<GeneralSettings>(options =>
            {
                options.HostName = "the-hostname";
                options.ExternalAppBaseUrl = "https://{org}.apps.{hostName}/{org}/{app}/";
            });
        });
        fixture.AppMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(appMetadata);

        // Act
        var result = fixture.FiksArkivConfigResolver.GetRecipientParty(instance, recipient);

        // Assert
        Assert.NotNull(result);
        var serialized = result.SerializeXmlBytes(indent: true);
        var xml = Encoding.UTF8.GetString(serialized.Span);
        var filename = nameof(GetRecipientParty_ReturnsExpectedValue) + "." + (orgNumber is null ? "partial" : "full");
        await Verify(xml).UseMethodName(filename);
    }
}
