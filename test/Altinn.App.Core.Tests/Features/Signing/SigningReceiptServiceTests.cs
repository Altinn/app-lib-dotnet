using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing;

public class SigningReceiptServiceTests
{
    private readonly IOptions<GeneralSettings> _generalSettings;

    public SigningReceiptServiceTests()
    {
        _generalSettings = Microsoft.Extensions.Options.Options.Create(new GeneralSettings());
    }

    SigningReceiptService SetupService(
        Mock<ICorrespondenceClient>? correspondenceClientMockOverride = null,
        Mock<IHostEnvironment>? hostEnvironmentMockOverride = null,
        Mock<IDataClient>? dataClientMockOverride = null,
        Mock<IAppResources>? appResourcesMockOverride = null,
        Mock<IAppMetadata>? appMetadataMockOverride = null
    )
    {
        Mock<ICorrespondenceClient> correspondenceClientMock = correspondenceClientMockOverride ?? new();
        Mock<IHostEnvironment> hostEnvironmentMock = hostEnvironmentMockOverride ?? new();
        Mock<IDataClient>? dataClientMock = dataClientMockOverride ?? new();
        Mock<IAppResources> appResourcesMock = appResourcesMockOverride ?? new();
        Mock<IAppMetadata> appMetadataMock = appMetadataMockOverride ?? new();
        Mock<ILogger<SigningReceiptService>> loggerMock = new();
        return new SigningReceiptService(
            correspondenceClientMock.Object,
            dataClientMock.Object,
            hostEnvironmentMock.Object,
            appResourcesMock.Object,
            appMetadataMock.Object,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task GetCorrespondenceAttachments_ReturnsCorrectAttachments()
    {
        // Arrange
        InstanceIdentifier instanceIdentifier = new(123456, Guid.NewGuid());

        // Create two data elements; only one will have a corresponding signature.
        DataElement signedElement = new()
        {
            Id = "11111111-1111-1111-1111-111111111111",
            Filename = "signed.pdf",
            ContentType = "application/pdf",
            DataType = "someType",
        };

        DataElement unsignedElement = new()
        {
            Id = "22222222-2222-2222-2222-222222222222",
            Filename = "unsigned.pdf",
            ContentType = "application/pdf",
            DataType = "someType",
        };

        Instance instance = new() { Data = [signedElement, unsignedElement] };

        Mock<IInstanceDataMutator> instanceDataMutatorMock = new();
        instanceDataMutatorMock.Setup(x => x.Instance).Returns(instance);
        UserActionContext context = new(instanceDataMutatorMock.Object, 123456);
        IEnumerable<DataElementSignature> dataElementSignatures = [new DataElementSignature(signedElement.Id)];

        ApplicationMetadata appMetadata = new("org/app");

        var dataClientMock = new Mock<IDataClient>();
        dataClientMock
            .Setup(x =>
                x.GetDataBytes(
                    It.Is<string>(org => org == appMetadata.AppIdentifier.Org),
                    It.Is<string>(app => app == appMetadata.AppIdentifier.App),
                    It.Is<int>(party => party == instanceIdentifier.InstanceOwnerPartyId),
                    It.Is<Guid>(guid => guid == instanceIdentifier.InstanceGuid),
                    It.Is<Guid>(id => id == Guid.Parse(signedElement.Id))
                )
            )
            .ReturnsAsync([1, 2, 3]);

        // Act
        IEnumerable<CorrespondenceAttachment> attachments = await SigningReceiptService.GetCorrespondenceAttachments(
            instanceIdentifier,
            dataElementSignatures,
            appMetadata,
            context,
            dataClientMock.Object
        );

        // Assert
        Assert.Single(attachments);
        CorrespondenceAttachment attachment = attachments.First();
        Assert.Equal("signed.pdf", attachment.Filename);
        Assert.Equal("signed.pdf", attachment.Name);
        Assert.Equal(signedElement.Id, attachment.SendersReference);
        Assert.Equal("application/pdf", attachment.DataType);
        Assert.Equal(new byte[] { 1, 2, 3 }, attachment.Data);
    }

    [Fact]
    public void GetDataElementFilename_DataElementHasFileName_ReturnsCorrectFilename()
    {
        // Arrange
        DataElement dataElement = new() { Filename = "filename" };
        ApplicationMetadata applicationMetadata = new("org/app");

        // Act
        string result = SigningReceiptService.GetDataElementFilename(dataElement, applicationMetadata);

        // Assert
        Assert.Equal("filename", result);
    }

    [Fact]
    public void GetDataElementFilename_NoFileNameNoApplogic_ReturnsConstructedFilename()
    {
        // Arrange
        DataElement dataElement = new()
        {
            // No filename set
            DataType = "typeToMatch",
            ContentType = "application/pdf",
        };
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            DataTypes =
            [
                new DataType
                {
                    Id = "typeToMatch",
                    Description = new LanguageString { { LanguageConst.En, "description" } },
                },
            ],
        };

        // Act
        string result = SigningReceiptService.GetDataElementFilename(dataElement, applicationMetadata);

        // Assert
        Assert.Equal("typeToMatch.pdf", result);
    }

    [Fact]
    public void GetDataElementFilename_NoFileName_HasApplogic_ReturnsConstructedFilename()
    {
        // Arrange
        DataElement dataElement = new()
        {
            // No filename set
            DataType = "typeToMatch",
            ContentType = "application/xml",
        };
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            DataTypes =
            [
                new DataType
                {
                    Id = "typeToMatch",
                    Description = new LanguageString { { LanguageConst.En, "description" } },
                    AppLogic = new ApplicationLogic { AutoCreate = true },
                },
            ],
        };

        // Act
        string result = SigningReceiptService.GetDataElementFilename(dataElement, applicationMetadata);

        // Assert
        Assert.Equal("skjemadata_typeToMatch.xml", result);
    }

    [Theory]
    [InlineData("The signed documents are attached.", LanguageConst.En)]
    [InlineData("Dokumenta du har signert er vedlagde.", LanguageConst.Nn)]
    [InlineData("Dokumentene du har signert er vedlagt.", LanguageConst.Nb)]
    [InlineData("Dokumentene du har signert er vedlagt.", "NotALanguage")]
    public void GetDefaultTexts_ReturnsCorrectTexts(string expectedBodyContains, string language)
    {
        // Arrange
        string appName = "appName";
        string appOwner = "appOwner";

        // Act
        DefaultTexts result = SigningReceiptService.GetDefaultTexts(language, appName, appOwner);

        // Assert
        Assert.Contains(expectedBodyContains, result.Body);
    }
}
