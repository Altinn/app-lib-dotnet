using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing.Notifications;

public class SigningNotificationServiceTests
{
    Mock<ILogger<SigningNotificationService>> _loggerMock = new(MockBehavior.Strict);

    private Mock<ISmsNotificationClient> SetupSmsNotificationClientMock(string orderId = "123")
    {
        Mock<ISmsNotificationClient> smsNotificationClientMock = new(MockBehavior.Strict);
        smsNotificationClientMock
            .Setup(x => x.Order(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsOrderResponse(OrderId: orderId));

        return smsNotificationClientMock;
    }

    private Mock<IEmailNotificationClient> SetupEmailNotificationClientMock(string orderId = "123")
    {
        Mock<IEmailNotificationClient> emailNotificationClientMock = new(MockBehavior.Strict);
        emailNotificationClientMock
            .Setup(x => x.Order(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailOrderResponse(OrderId: orderId));

        return emailNotificationClientMock;
    }

    [Fact]
    public async Task NotifySignatureTask_WhenPersonSignee_SmsNotificationSent()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: false,
                mobileNumber: "12345678",
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // No error message
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
    }

    [Fact]
    public async Task NotifySignatureTask_WhenOrganisationSignee_SmsNotificationSent()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: true,
                mobileNumber: "12345678",
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // No error message
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
    }

    [Fact]
    public async Task NotifySignatureTask_WhenPersonSignee_EmailNotificationSent()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            emailNotificationClient: SetupEmailNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupEmailSigneeContextNotification(
                isOrganisation: false,
                email: "test@test.no",
                subject: "Test Email",
                body: "This is a test email for testing purposes"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // No error message
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
    }

    [Fact]
    public async Task NotifySignatureTask_WhenOrganisationSignee_EmailNotificationSent()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            emailNotificationClient: SetupEmailNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupEmailSigneeContextNotification(
                isOrganisation: true,
                email: "test@test.no",
                subject: "Test Email",
                body: "This is a test email for testing purposes"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // No error message
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
    }

    [Fact]
    public async Task NotifySignatureTask_WhenSmsAndEmailConfigured_SmsAndEmailNotificationSent()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object,
            emailNotificationClient: SetupEmailNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: false,
                mobileNumber: "12345678",
                body: "Test SMS",
                reference: "sms-reference"
            ),
            SetupEmailSigneeContextNotification(
                isOrganisation: false,
                email: "test@test.no",
                subject: "Test Email",
                body: "This is a test email for testing purposes"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // No error message
        Assert.True(signeeContexts[1].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[1].SigneeState.SignatureRequestEmailNotSentReason); // No error message
    }

    [Fact]
    public async Task NotifySignatureTask_WhenNotificationAlreadySent_Success()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object,
            emailNotificationClient: SetupEmailNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: false,
                mobileNumber: "12345678",
                body: "Test SMS",
                reference: "sms-reference",
                hasSentSms: true
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // No error message
    }

    private SigneeContext SetupSmsSigneeContextNotification(
        bool isOrganisation,
        string mobileNumber,
        string? body,
        string reference,
        bool? hasSentSms = false
    )
    {
        return new SigneeContext
        {
            Party = new Party(),
            TaskId = "task-id",
            SigneeState = new SigneeState { SignatureRequestSmsSent = hasSentSms ?? false },
            OrganisationSignee = isOrganisation
                ? new OrganisationSignee
                {
                    DisplayName = "Test Organisation",
                    OrganisationNumber = "123456789",
                    Notifications = new Core.Features.Signing.Models.Notifications
                    {
                        OnSignatureAccessRightsDelegated = new Notification
                        {
                            Sms = new Sms
                            {
                                MobileNumber = mobileNumber,
                                Body = body,
                                Reference = reference,
                            },
                        },
                    },
                }
                : null,
            PersonSignee = isOrganisation
                ? null
                : new PersonSignee
                {
                    DisplayName = "Test Person",
                    SocialSecurityNumber = "123456789",
                    FullName = "Test Person",
                    Notifications = new Core.Features.Signing.Models.Notifications
                    {
                        OnSignatureAccessRightsDelegated = new Notification
                        {
                            Sms = new Sms
                            {
                                MobileNumber = mobileNumber,
                                Body = body,
                                Reference = reference,
                            },
                        },
                    },
                },
        };
    }

    private SigneeContext SetupEmailSigneeContextNotification(
        bool isOrganisation,
        string email,
        string? subject,
        string? body,
        bool? hasSentEmail = false
    )
    {
        return new SigneeContext
        {
            Party = new Party(),
            TaskId = "task-id",
            SigneeState = new SigneeState { SignatureRequestEmailSent = hasSentEmail ?? false },
            OrganisationSignee = isOrganisation
                ? new OrganisationSignee
                {
                    DisplayName = "Test Organisation",
                    OrganisationNumber = "123456789",
                    Notifications = new Core.Features.Signing.Models.Notifications
                    {
                        OnSignatureAccessRightsDelegated = new Notification
                        {
                            Email = new Email
                            {
                                EmailAddress = email,
                                Subject = subject,
                                Body = body,
                            },
                        },
                    },
                }
                : null,
            PersonSignee = isOrganisation
                ? null
                : new PersonSignee
                {
                    DisplayName = "Test Person",
                    SocialSecurityNumber = "123456789",
                    FullName = "Test Person",
                    Notifications = new Core.Features.Signing.Models.Notifications
                    {
                        OnSignatureAccessRightsDelegated = new Notification
                        {
                            Email = new Email
                            {
                                EmailAddress = email,
                                Subject = subject,
                                Body = body,
                            },
                        },
                    },
                },
        };
    }
}
