using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing;

public class SigningCallToActionServiceTests
{
    static SigningCallToActionService SetupService(Mock<IAppResources>? appResourcesMockOverride = null)
    {
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        Mock<IAppResources> appResourcesMock = appResourcesMockOverride ?? new();
        Mock<IAppMetadata> appMetadataMock = new();
        Mock<IProfileClient> profileClientMock = new();
        Mock<ILogger<SigningCallToActionService>> loggerMock = new();
        Mock<IOptions<GeneralSettings>> generalSettingsMock = new();
        return new SigningCallToActionService(
            correspondenceClientMock.Object,
            hostEnvironmentMock.Object,
            appResourcesMock.Object,
            appMetadataMock.Object,
            profileClientMock.Object,
            loggerMock.Object,
            generalSettingsMock.Object
        );
    }

    Mock<IAppResources> SetupAppResourcesMock(
        TextResource? textResourceOverride = null,
        List<TextResourceElement>? additionalTextResourceElements = null
    )
    {
        Mock<IAppResources> appResourcesMock = new();
        TextResource textResource =
            textResourceOverride
            ?? new TextResource
            {
                Resources =
                [
                    new TextResourceElement { Id = "signing.cta_title", Value = "Custom title" },
                    new TextResourceElement { Id = "signing.cta_summary", Value = "Custom summary" },
                    new TextResourceElement
                    {
                        Id = "signing.cta_body",
                        Value =
                            "Custom body with replacement for instance url here: $InstanceUrl, and some more text after",
                    },
                ],
            };
        textResource.Resources.AddRange(additionalTextResourceElements ?? []);
        appResourcesMock
            .Setup(m => m.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(textResource);
        return appResourcesMock;
    }

    [Fact]
    public async Task GetContent_WithCustomTexts_ReturnsCorrectContent()
    {
        // Arrange
        string smsContentTextResourceKey = "signing.sms_content";
        List<TextResourceElement> smsTextResource =
        [
            new TextResourceElement { Id = smsContentTextResourceKey, Value = "Custom sms content" },
        ];
        Mock<IAppResources> mock = SetupAppResourcesMock(additionalTextResourceElements: smsTextResource);
        SigningCallToActionService service = SetupService(appResourcesMockOverride: mock);
        Notification notification = new()
        {
            Sms = new Sms { MobileNumber = "12345678", TextResourceKey = smsContentTextResourceKey },
        };
        AppIdentifier appIdentifier = new("org", "app");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.En, "App name" } },
        };
        Party sendersParty = new() { Name = "Sender" };
        string instanceUrl = "https://altinn.local.cloud";
        string language = LanguageConst.En;

        // Act
        ContentWrapper res = await service.GetContent(
            notification,
            appIdentifier,
            applicationMetadata,
            sendersParty,
            instanceUrl,
            language
        );

        // Assert
        Assert.Equal("Custom sms content", res.SmsBody);
        Assert.Null(res.EmailBody);
        Assert.Null(res.EmailSubject);
        Assert.Equal(language, res.CorrespondenceContent.Language);
        Assert.Equal("Custom title", res.CorrespondenceContent.Title);
        Assert.Equal("Custom summary", res.CorrespondenceContent.Summary);
        Assert.Equal(
            "Custom body with replacement for instance url here: https://altinn.local.cloud, and some more text after",
            res.CorrespondenceContent.Body
        );
    }

    [Fact]
    public async Task GetContent_GetTextsThrows_UsesDefaultTexts()
    {
        // Arrange
        Mock<IAppResources> mock = SetupAppResourcesMock();
        mock.Setup(m => m.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        SigningCallToActionService service = SetupService(appResourcesMockOverride: mock);
        Notification notification = new()
        {
            Sms = new Sms { MobileNumber = "12345678", TextResourceKey = "signing.sms_content" },
        };
        AppIdentifier appIdentifier = new("org", "app");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.En, "App name" } },
        };
        Party sendersParty = new() { Name = "Sender" };
        string instanceUrl = "https://altinn.local.cloud";
        string language = LanguageConst.En;

        // Act
        ContentWrapper res = await service.GetContent(
            notification,
            appIdentifier,
            applicationMetadata,
            sendersParty,
            instanceUrl,
            language
        );

        // Assert
        Assert.Contains("Your signature is requested for", res.SmsBody);
        Assert.Contains("You have a task waiting for your signature.", res.CorrespondenceContent.Body);
    }

    [Theory]
    [InlineData("You have a task waiting for your signature.", LanguageConst.En)]
    [InlineData("Du har ei oppgåve som ventar på signaturen din.", LanguageConst.Nn)]
    [InlineData("Du har en oppgave som venter på din signatur.", LanguageConst.Nb)]
    [InlineData("Du har en oppgave som venter på din signatur.", "NotALanguage")]
    public void GetDefaultTexts_ReturnsCorrectTexts(string expectedBodyContains, string language)
    {
        // Arrange
        string instanceUrl = "https://altinn.local.cloud";
        string appName = "appName";
        string appOwner = "appOwner";

        // Act
        DefaultTexts result = SigningCallToActionService.GetDefaultTexts(instanceUrl, language, appName, appOwner);

        // Assert
        Assert.NotNull(result.SmsBody);
        Assert.NotNull(result.EmailBody);
        Assert.NotNull(result.EmailSubject);
        Assert.Contains(expectedBodyContains, result.Body);
    }
}
