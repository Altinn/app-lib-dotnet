using Altinn.App.Core.Configuration;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
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
    private readonly IOptions<GeneralSettings> _generalSettings;

    public SigningCallToActionServiceTests()
    {
        _generalSettings = Microsoft.Extensions.Options.Options.Create(new GeneralSettings());
    }

    SigningCallToActionService SetupService(
        Mock<ICorrespondenceClient>? correspondenceClientMockOverride = null,
        Mock<IHostEnvironment>? hostEnvironmentMockOverride = null,
        Mock<IAppResources>? appResourcesMockOverride = null,
        Mock<IAppMetadata>? appMetadataMockOverride = null,
        Mock<IProfileClient>? profileClientMockOverride = null
    )
    {
        Mock<ICorrespondenceClient> correspondenceClientMock = correspondenceClientMockOverride ?? new();
        Mock<IHostEnvironment> hostEnvironmentMock = hostEnvironmentMockOverride ?? new();
        Mock<IAppResources> appResourcesMock = appResourcesMockOverride ?? new();
        Mock<IAppMetadata> appMetadataMock = appMetadataMockOverride ?? new();
        Mock<IProfileClient> profileClientMock = profileClientMockOverride ?? new();
        Mock<ILogger<SigningCallToActionService>> loggerMock = new();
        return new SigningCallToActionService(
            correspondenceClientMock.Object,
            hostEnvironmentMock.Object,
            appResourcesMock.Object,
            appMetadataMock.Object,
            profileClientMock.Object,
            loggerMock.Object,
            _generalSettings
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
                    new TextResourceElement { Id = "signing.correspondence_cta_title", Value = "Custom title" },
                    new TextResourceElement { Id = "signing.correspondence_cta_summary", Value = "Custom summary" },
                    new TextResourceElement
                    {
                        Id = "signing.correspondence_cta_body",
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

    /// <summary>
    /// Test case: SendSignCallToAction with SMS notification configured.
    /// Expected: SMS used, CorrespondenceClient is called with correct parameters.
    /// </summary>
    [Fact]
    public async Task SendSignCallToAction_SmsNotificationConfigured_CallsCorrespondenceClientWithCorrectParameters()
    {
        // Arrange
        string smsContentTextResourceKey = "signing.sms_content";
        SendCorrespondencePayload? capturedPayload = null;
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        correspondenceClientMock
            .Setup(m => m.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()))
            .Callback<SendCorrespondencePayload, CancellationToken>((payload, token) => capturedPayload = payload);
        List<TextResourceElement> smsTextResource =
        [
            new TextResourceElement { Id = smsContentTextResourceKey, Value = "Custom sms content" },
        ];
        Mock<IAppResources> appResourcesMock = SetupAppResourcesMock(additionalTextResourceElements: smsTextResource);
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        hostEnvironmentMock.Setup(m => m.EnvironmentName).Returns("tt02");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.Nb, "TestAppName" } },
        };
        Mock<IAppMetadata> appMetadataMock = new();
        appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        SigningCallToActionService service = SetupService(
            correspondenceClientMockOverride: correspondenceClientMock,
            appResourcesMockOverride: appResourcesMock,
            appMetadataMockOverride: appMetadataMock,
            hostEnvironmentMockOverride: hostEnvironmentMock
        );

        Notification notification = new()
        {
            Sms = new Sms { MobileNumber = "12345678", TextResourceKey = smsContentTextResourceKey },
        };
        AppIdentifier appIdentifier = new("org", "app");
        InstanceIdentifier instanceIdentifier = new(123, Guid.Parse("ab0cdeb5-dc5e-4faa-966b-d18bb932ca07"));
        Party signingParty = new() { Name = "Signee", SSN = "17858296439" };
        Party serviceOwnerParty = new() { Name = "Service owner", OrgNumber = "043871668" };
        List<AltinnEnvironmentConfig> correspondenceResources =
        [
            new AltinnEnvironmentConfig { Environment = "tt02", Value = "app_ttd_appname" },
        ];

        // Act
        await service.SendSignCallToAction(
            notification,
            appIdentifier,
            instanceIdentifier,
            signingParty,
            serviceOwnerParty,
            correspondenceResources
        );

        // Assert
        Assert.NotNull(capturedPayload);
        Assert.Equal(
            CorrespondenceNotificationChannel.Sms,
            capturedPayload.CorrespondenceRequest.Notification!.NotificationChannel
        );
        Assert.Equal("Custom sms content", capturedPayload.CorrespondenceRequest.Notification.SmsBody);
        Assert.Null(capturedPayload.CorrespondenceRequest.Notification.EmailBody);
        Assert.Null(capturedPayload.CorrespondenceRequest.Notification.EmailSubject);
        Assert.Equal("Custom title", capturedPayload.CorrespondenceRequest.Content.Title);
        Assert.Equal("Custom summary", capturedPayload.CorrespondenceRequest.Content.Summary);
        Assert.Equal(
            "Custom body with replacement for instance url here: http://local.altinn.cloud/org/app#/123/ab0cdeb5-dc5e-4faa-966b-d18bb932ca07, and some more text after",
            capturedPayload.CorrespondenceRequest.Content.Body
        );
        Assert.Equal("app_ttd_appname", capturedPayload.CorrespondenceRequest.ResourceId);
        Assert.Equal("043871668", capturedPayload.CorrespondenceRequest.Sender.ToString());
        Assert.IsType<OrganisationOrPersonIdentifier.Person>(capturedPayload.CorrespondenceRequest.Recipients[0]);
        Assert.Equal(
            "17858296439",
            (capturedPayload.CorrespondenceRequest.Recipients[0] as OrganisationOrPersonIdentifier.Person)!.ToString()
        );
    }

    /// <summary>
    /// Test case: SendSignCallToAction with Email notification configured.
    /// Expected: Email used, CorrespondenceClient is called with correct parameters.
    /// </summary>
    [Fact]
    public async Task SendSignCallToAction_EmailNotificationConfigured_CallsCorrespondenceClientWithCorrectParameters()
    {
        // Arrange
        SendCorrespondencePayload? capturedPayload = null;
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        correspondenceClientMock
            .Setup(m => m.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()))
            .Callback<SendCorrespondencePayload, CancellationToken>((payload, token) => capturedPayload = payload);
        List<TextResourceElement> smsTextResource =
        [
            new TextResourceElement { Id = "signing.email_subject", Value = "Custom email subject" },
            new TextResourceElement { Id = "signing.email_content", Value = "Custom email content" },
        ];
        Mock<IAppResources> appResourcesMock = SetupAppResourcesMock(additionalTextResourceElements: smsTextResource);
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        hostEnvironmentMock.Setup(m => m.EnvironmentName).Returns("tt02");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.Nb, "TestAppName" } },
        };
        Mock<IAppMetadata> appMetadataMock = new();
        appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        SigningCallToActionService service = SetupService(
            correspondenceClientMockOverride: correspondenceClientMock,
            appResourcesMockOverride: appResourcesMock,
            appMetadataMockOverride: appMetadataMock,
            hostEnvironmentMockOverride: hostEnvironmentMock
        );

        Notification notification = new()
        {
            Email = new Email
            {
                EmailAddress = "my.email@test.no",
                BodyTextResourceKey = "signing.email_content",
                SubjectTextResourceKey = "signing.email_subject",
            },
        };
        AppIdentifier appIdentifier = new("org", "app");
        InstanceIdentifier instanceIdentifier = new(123, Guid.Parse("ab0cdeb5-dc5e-4faa-966b-d18bb932ca07"));
        Party signingParty = new() { Name = "Signee", SSN = "17858296439" };
        Party serviceOwnerParty = new() { Name = "Service owner", OrgNumber = "043871668" };
        List<AltinnEnvironmentConfig> correspondenceResources =
        [
            new AltinnEnvironmentConfig { Environment = "tt02", Value = "app_ttd_appname" },
        ];

        // Act
        await service.SendSignCallToAction(
            notification,
            appIdentifier,
            instanceIdentifier,
            signingParty,
            serviceOwnerParty,
            correspondenceResources
        );

        // Assert
        Assert.NotNull(capturedPayload);
        Assert.Equal(
            CorrespondenceNotificationChannel.Email,
            capturedPayload.CorrespondenceRequest.Notification!.NotificationChannel
        );
        Assert.Null(capturedPayload.CorrespondenceRequest.Notification.SmsBody);
        Assert.Equal("Custom email content", capturedPayload.CorrespondenceRequest.Notification.EmailBody);
        Assert.Equal("Custom email subject", capturedPayload.CorrespondenceRequest.Notification.EmailSubject);
        Assert.Equal("Custom title", capturedPayload.CorrespondenceRequest.Content.Title);
        Assert.Equal("Custom summary", capturedPayload.CorrespondenceRequest.Content.Summary);
        Assert.Equal(
            "Custom body with replacement for instance url here: http://local.altinn.cloud/org/app#/123/ab0cdeb5-dc5e-4faa-966b-d18bb932ca07, and some more text after",
            capturedPayload.CorrespondenceRequest.Content.Body
        );
        Assert.Equal("app_ttd_appname", capturedPayload.CorrespondenceRequest.ResourceId);
        Assert.Equal("043871668", capturedPayload.CorrespondenceRequest.Sender.ToString());
        Assert.IsType<OrganisationOrPersonIdentifier.Person>(capturedPayload.CorrespondenceRequest.Recipients[0]);
        Assert.Equal(
            "17858296439",
            (capturedPayload.CorrespondenceRequest.Recipients[0] as OrganisationOrPersonIdentifier.Person)!.ToString()
        );
    }

    /// <summary>
    /// Test case: SendSignCallToAction with both Email and SMS notification configured.
    /// Expected: Email is preferred, CorrespondenceClient is called with correct parameters.
    /// </summary>
    [Fact]
    public async Task SendSignCallToAction_SmsAndEmailNotificationConfigured_CallsCorrespondenceClientWithCorrectParameters()
    {
        // Arrange
        string smsContentTextResourceKey = "signing.sms_content";
        SendCorrespondencePayload? capturedPayload = null;
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        correspondenceClientMock
            .Setup(m => m.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()))
            .Callback<SendCorrespondencePayload, CancellationToken>((payload, token) => capturedPayload = payload);
        List<TextResourceElement> smsTextResource =
        [
            new TextResourceElement { Id = smsContentTextResourceKey, Value = "Custom sms content" },
            new TextResourceElement { Id = "signing.email_subject", Value = "Custom email subject" },
            new TextResourceElement { Id = "signing.email_content", Value = "Custom email content" },
        ];
        Mock<IAppResources> appResourcesMock = SetupAppResourcesMock(additionalTextResourceElements: smsTextResource);
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        hostEnvironmentMock.Setup(m => m.EnvironmentName).Returns("tt02");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.Nb, "TestAppName" } },
        };
        Mock<IAppMetadata> appMetadataMock = new();
        appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        SigningCallToActionService service = SetupService(
            correspondenceClientMockOverride: correspondenceClientMock,
            appResourcesMockOverride: appResourcesMock,
            appMetadataMockOverride: appMetadataMock,
            hostEnvironmentMockOverride: hostEnvironmentMock
        );

        Notification notification = new()
        {
            Sms = new Sms { MobileNumber = "12345678", TextResourceKey = smsContentTextResourceKey },
            Email = new Email
            {
                EmailAddress = "my.email@test.no",
                BodyTextResourceKey = "signing.email_content",
                SubjectTextResourceKey = "signing.email_subject",
            },
        };
        AppIdentifier appIdentifier = new("org", "app");
        InstanceIdentifier instanceIdentifier = new(123, Guid.Parse("ab0cdeb5-dc5e-4faa-966b-d18bb932ca07"));
        Party signingParty = new() { Name = "Signee", SSN = "17858296439" };
        Party serviceOwnerParty = new() { Name = "Service owner", OrgNumber = "043871668" };
        List<AltinnEnvironmentConfig> correspondenceResources =
        [
            new AltinnEnvironmentConfig { Environment = "tt02", Value = "app_ttd_appname" },
        ];

        // Act
        await service.SendSignCallToAction(
            notification,
            appIdentifier,
            instanceIdentifier,
            signingParty,
            serviceOwnerParty,
            correspondenceResources
        );

        // Assert
        Assert.NotNull(capturedPayload);
        Assert.Equal(
            CorrespondenceNotificationChannel.EmailPreferred,
            capturedPayload.CorrespondenceRequest.Notification!.NotificationChannel
        );
        Assert.Equal("Custom sms content", capturedPayload.CorrespondenceRequest.Notification.SmsBody);
        Assert.Equal("Custom email content", capturedPayload.CorrespondenceRequest.Notification.EmailBody);
        Assert.Equal("Custom email subject", capturedPayload.CorrespondenceRequest.Notification.EmailSubject);
        Assert.Equal("Custom title", capturedPayload.CorrespondenceRequest.Content.Title);
        Assert.Equal("Custom summary", capturedPayload.CorrespondenceRequest.Content.Summary);
        Assert.Equal(
            "Custom body with replacement for instance url here: http://local.altinn.cloud/org/app#/123/ab0cdeb5-dc5e-4faa-966b-d18bb932ca07, and some more text after",
            capturedPayload.CorrespondenceRequest.Content.Body
        );
        Assert.Equal("app_ttd_appname", capturedPayload.CorrespondenceRequest.ResourceId);
        Assert.Equal("043871668", capturedPayload.CorrespondenceRequest.Sender.ToString());
        Assert.IsType<OrganisationOrPersonIdentifier.Person>(capturedPayload.CorrespondenceRequest.Recipients[0]);
        Assert.Equal(
            "17858296439",
            (capturedPayload.CorrespondenceRequest.Recipients[0] as OrganisationOrPersonIdentifier.Person)!.ToString()
        );
    }

    /// <summary>
    /// Test case: SendSignCallToAction with no notification configured.
    /// Expected: Email with default text used. CorrespondenceClient is called with correct parameters.
    /// </summary>
    [Fact]
    public async Task SendSignCallToAction_NotificationNotConfigured_CallsCorrespondenceClientWithCorrectParameters()
    {
        // Arrange
        SendCorrespondencePayload? capturedPayload = null;
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        correspondenceClientMock
            .Setup(m => m.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()))
            .Callback<SendCorrespondencePayload, CancellationToken>((payload, token) => capturedPayload = payload);
        Mock<IAppResources> appResourcesMock = SetupAppResourcesMock();
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        hostEnvironmentMock.Setup(m => m.EnvironmentName).Returns("tt02");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.Nb, "TestAppName" } },
        };
        Mock<IAppMetadata> appMetadataMock = new();
        appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        SigningCallToActionService service = SetupService(
            correspondenceClientMockOverride: correspondenceClientMock,
            appResourcesMockOverride: appResourcesMock,
            appMetadataMockOverride: appMetadataMock,
            hostEnvironmentMockOverride: hostEnvironmentMock
        );

        Notification notification = new() { };
        AppIdentifier appIdentifier = new("org", "app");
        InstanceIdentifier instanceIdentifier = new(123, Guid.Parse("ab0cdeb5-dc5e-4faa-966b-d18bb932ca07"));
        Party signingParty = new() { Name = "Signee", SSN = "17858296439" };
        Party serviceOwnerParty = new() { Name = "Service owner", OrgNumber = "043871668" };
        List<AltinnEnvironmentConfig> correspondenceResources =
        [
            new AltinnEnvironmentConfig { Environment = "tt02", Value = "app_ttd_appname" },
        ];

        // Act
        await service.SendSignCallToAction(
            notification,
            appIdentifier,
            instanceIdentifier,
            signingParty,
            serviceOwnerParty,
            correspondenceResources
        );

        // Assert
        Assert.NotNull(capturedPayload);
        Assert.Equal(
            CorrespondenceNotificationChannel.Email,
            capturedPayload.CorrespondenceRequest.Notification!.NotificationChannel
        );
        Assert.Null(capturedPayload.CorrespondenceRequest.Notification.SmsBody);
        Assert.Equal(
            "Din signatur ventes for TestAppName. Åpne Altinn-innboksen din for å fortsette.<br /><br />Hvis du lurer på noe, kan du kontakte Service owner.",
            capturedPayload.CorrespondenceRequest.Notification.EmailBody
        );
        Assert.Equal(
            "TestAppName: Oppgave til signering",
            capturedPayload.CorrespondenceRequest.Notification.EmailSubject
        );
        Assert.Equal("Custom title", capturedPayload.CorrespondenceRequest.Content.Title);
        Assert.Equal("Custom summary", capturedPayload.CorrespondenceRequest.Content.Summary);
        Assert.Equal(
            "Custom body with replacement for instance url here: http://local.altinn.cloud/org/app#/123/ab0cdeb5-dc5e-4faa-966b-d18bb932ca07, and some more text after",
            capturedPayload.CorrespondenceRequest.Content.Body
        );
        Assert.Equal("app_ttd_appname", capturedPayload.CorrespondenceRequest.ResourceId);
        Assert.Equal("043871668", capturedPayload.CorrespondenceRequest.Sender.ToString());
        Assert.IsType<OrganisationOrPersonIdentifier.Person>(capturedPayload.CorrespondenceRequest.Recipients[0]);
        Assert.Equal(
            "17858296439",
            (capturedPayload.CorrespondenceRequest.Recipients[0] as OrganisationOrPersonIdentifier.Person)!.ToString()
        );
    }

    /// <summary>
    /// Test case: resource for correspondence not found.
    /// Expected: Exception thrown.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SendSignCallToAction_ResourceNotFound_ThrowsException()
    {
        // Arrange
        Mock<ICorrespondenceClient> correspondenceClientMock = new();
        Mock<IAppResources> appResourcesMock = new();
        appResourcesMock
            .Setup(m => m.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TextResource());
        Mock<IHostEnvironment> hostEnvironmentMock = new();
        hostEnvironmentMock.Setup(m => m.EnvironmentName).Returns("tt02");
        ApplicationMetadata applicationMetadata = new("org/app")
        {
            Title = new Dictionary<string, string> { { LanguageConst.Nb, "TestAppName" } },
        };
        Mock<IAppMetadata> appMetadataMock = new();
        appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        SigningCallToActionService service = SetupService(
            correspondenceClientMockOverride: correspondenceClientMock,
            appResourcesMockOverride: appResourcesMock,
            appMetadataMockOverride: appMetadataMock,
            hostEnvironmentMockOverride: hostEnvironmentMock
        );

        Notification notification = new() { };
        AppIdentifier appIdentifier = new("org", "app");
        InstanceIdentifier instanceIdentifier = new(123, Guid.Parse("ab0cdeb5-dc5e-4faa-966b-d18bb932ca07"));
        Party signingParty = new() { Name = "Signee", SSN = "17858296439" };
        Party serviceOwnerParty = new() { Name = "Service owner", OrgNumber = "043871668" };
        List<AltinnEnvironmentConfig> correspondenceResources =
        [
            // No resource for tt02 - should throw exception
        ];

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationException>(
            async () =>
                await service.SendSignCallToAction(
                    notification,
                    appIdentifier,
                    instanceIdentifier,
                    signingParty,
                    serviceOwnerParty,
                    correspondenceResources
                )
        );
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
        string defaultEmailSubjectContains = "Task for signing";
        string defaultEmailBodyContains = "Your signature is requested for";

        // Assert
        Assert.Equal("Custom sms content", res.SmsBody);
        Assert.Contains(defaultEmailSubjectContains, res.EmailSubject);
        Assert.Contains(defaultEmailBodyContains, res.EmailBody);
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
        Party sendersParty = new() { Name = "SenderNameForTest" };
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
        Assert.Contains(instanceUrl, res.CorrespondenceContent.Body);
        Assert.Contains(sendersParty.Name, res.CorrespondenceContent.Body);
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
