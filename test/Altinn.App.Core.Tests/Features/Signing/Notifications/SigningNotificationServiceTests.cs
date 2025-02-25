using System.Net;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Logging;
using Moq;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using static Altinn.App.Core.Features.Signing.SigningNotificationService;

namespace Altinn.App.Core.Tests.Features.Signing.Notifications;

public class SigningNotificationServiceTests
{
    Mock<ILogger<SigningNotificationService>> _loggerMock = new();

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
                mobileNumber: "+4712345678",
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
                mobileNumber: "004712345678",
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
            emailNotificationClient: SetupEmailNotificationClientMock().Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            new()
            {
                TaskId = "task-id",
                Signee = new PersonSignee
                {
                    Party = new Party { },
                    SocialSecurityNumber = "123456789",
                    FullName = "Test Person",
                },
                SigneeState = new SigneeState { SignatureRequestSmsSent = false },
                Notifications = new Core.Features.Signing.Models.Notifications
                {
                    OnSignatureAccessRightsDelegated = new Notification
                    {
                        Sms = new Sms
                        {
                            MobileNumber = "+4512345678",
                            Body = "Test SMS",
                            Reference = "sms-reference",
                        },
                        Email = new Email
                        {
                            EmailAddress = "test@test.no",
                            Subject = "Test Email",
                            Body = "This is a test email for testing purposes",
                        },
                    },
                },
            },
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // No error message
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // No error message
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
                mobileNumber: "+4712345678",
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

    [Fact]
    public async Task NotifySignatureTask_TrySendSmsWithoutClient_Fails()
    {
        SigningNotificationService signingNotificationService = new(logger: _loggerMock.Object);
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: false,
                mobileNumber: "+4712345678",
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // Error message
        Assert.Equal(
            "No implementation of ISmsNotificationClient registered. Unable to send notification.",
            signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_TrySendSmsWithNoMobileNumber_Fails()
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
                mobileNumber: string.Empty, // No mobile number set
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // Error message
        Assert.Equal(
            "No mobile number provided. Unable to send SMS notification.",
            signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_TrySentSmsWithoutCountryCode_Succeeds()
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
                mobileNumber: "12345678", // No country code set
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // Error message
    }

    [Fact]
    public async Task NotifySignatureTask_TrySendEmailWithoutClient_Fails()
    {
        SigningNotificationService signingNotificationService = new(logger: _loggerMock.Object);
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
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // Error message
        Assert.Equal(
            "No implementation of IEmailNotificationClient registered. Unable to send notification.",
            signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_TrySendEmailWithNoEmailAddress_Fails()
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
                email: string.Empty, // No email address set
                subject: "Test Email",
                body: "This is a test email for testing purposes"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // Error message
        Assert.Equal(
            "No email address provided. Unable to send email notification.",
            signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_TrySendSmsWithException_Fails()
    {
        //

        Mock<ISmsNotificationClient> smsNotificationClientMock = new Mock<ISmsNotificationClient>(MockBehavior.Strict);
        smsNotificationClientMock
            .Setup(x => x.Order(It.IsAny<SmsNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new SmsNotificationException(
                    "Failed to send SMS notification",
                    new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ReasonPhrase = "Conflict",
                        Content = new StringContent("Failed to send SMS notification"),
                    },
                    "Failed to send SMS notification",
                    new Exception()
                )
            );

        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            smsNotificationClient: smsNotificationClientMock.Object
        );
        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            SetupSmsSigneeContextNotification(
                isOrganisation: false,
                mobileNumber: "+4712345678",
                body: "Test SMS",
                reference: "sms-reference"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // Error message
        Assert.Equal(
            "Failed to send SMS notification: Failed to send SMS notification: StatusCode=Conflict\nReason=Conflict\nBody=Failed to send SMS notification\n",
            signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_TrySendEmailWithException_Fails()
    {
        Mock<IEmailNotificationClient> emailNotificationClientMock = new(MockBehavior.Strict);
        emailNotificationClientMock
            .Setup(x => x.Order(It.IsAny<EmailNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new EmailNotificationException(
                    "Failed to send email notification",
                    new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ReasonPhrase = "Conflict",
                        Content = new StringContent("Failed to send email notification"),
                    },
                    "Failed to send email notification",
                    new Exception()
                )
            );

        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            emailNotificationClient: emailNotificationClientMock.Object
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
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // Error message
        Assert.Equal(
            "Failed to send Email notification: Failed to send email notification: StatusCode=Conflict\nReason=Conflict\nBody=Failed to send email notification\n",
            signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason
        );
    }

    [Fact]
    public async Task NotifySignatureTask_WhenNotificationFails_KeepProcessingRemainingNotifications()
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
                email: string.Empty, // No email address set
                subject: "Test Email",
                body: "This is a test email for testing purposes"
            ),
            SetupEmailSigneeContextNotification(
                isOrganisation: false,
                email: "test@test.no",
                subject: "Test Email2",
                body: "This is a test email for testing purposes"
            ),
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // Error message
        Assert.Equal(
            "No email address provided. Unable to send email notification.",
            signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason
        );

        Assert.True(signeeContexts[1].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[1].SigneeState.SignatureRequestEmailNotSentReason); // No error message
    }

    [Fact]
    public async Task NotifySignatureTask_WhenSmsFails_TrySendEmail()
    {
        SigningNotificationService signingNotificationService = new(
            logger: _loggerMock.Object,
            emailNotificationClient: SetupEmailNotificationClientMock().Object,
            smsNotificationClient: SetupSmsNotificationClientMock().Object
        );

        // Arrange
        var signeeContexts = new List<SigneeContext>
        {
            new()
            {
                TaskId = "task-id",
                Signee = new PersonSignee
                {
                    Party = new Party { },
                    SocialSecurityNumber = "123456789",
                    FullName = "Test Person",
                },
                SigneeState = new SigneeState { SignatureRequestSmsSent = false },
                Notifications = new Core.Features.Signing.Models.Notifications
                {
                    OnSignatureAccessRightsDelegated = new Notification
                    {
                        Sms = new Sms
                        {
                            MobileNumber = "", // No mobile number set, will fail
                            Body = "Test SMS",
                            Reference = "sms-reference",
                        },
                        Email = new Email
                        {
                            EmailAddress = "test@test.no",
                            Subject = "Test Email",
                            Body = "This is a test email for testing purposes",
                        },
                    },
                },
            },
        };

        // Act
        signeeContexts = await signingNotificationService.NotifySignatureTask(signeeContexts);

        // Assert
        Assert.False(signeeContexts[0].SigneeState.SignatureRequestSmsSent); // SMS notification not sent
        Assert.NotNull(signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason); // Error message
        Assert.Equal(
            "No mobile number provided. Unable to send SMS notification.",
            signeeContexts[0].SigneeState.SignatureRequestSmsNotSentReason
        );
        Assert.True(signeeContexts[0].SigneeState.SignatureRequestEmailSent); // Email notification sent
        Assert.Null(signeeContexts[0].SigneeState.SignatureRequestEmailNotSentReason); // No error message
    }

    [Fact]
    public void GetEmailBody_WhenNoBodySet_ReturnsDefaultBody()
    {
        // Arrange
        var email = new Email { EmailAddress = "test@test.no", Subject = "Test Email" };

        // Act
        string body = GetEmailBody(email);

        // Assert
        Assert.Equal(NotificationDefaults.EmailBody, body);
    }

    [Fact]
    public void GetEmailSubject_WhenNoSubjectSet_ReturnsDefaultSubject()
    {
        // Arrange
        var email = new Email { EmailAddress = "test@test.no", Body = "This is a test email for testing purposes" };

        // Act
        string subject = GetEmailSubject(email);

        // Assert
        Assert.Equal(NotificationDefaults.EmailSubject, subject);
    }

    [Fact]
    public void GetSmsBody_WhenNoBodySet_ReturnsDefaultBody()
    {
        // Arrange
        var sms = new Sms { MobileNumber = "+4712345678" };

        // Act
        string body = GetSmsBody(sms);

        // Assert
        Assert.Equal(NotificationDefaults.SmsBody, body);
    }

    private SigneeContext SetupSmsSigneeContextNotification(
        bool isOrganisation,
        string mobileNumber,
        string? body,
        string reference,
        bool? hasSentSms = false
    )
    {
        PersonSignee personSignee = new()
        {
            Party = new Party { },
            SocialSecurityNumber = "123456789",
            FullName = "Test Person",
        };
        OrganisationSignee organisationSignee = new()
        {
            OrgParty = new Party { },
            OrgNumber = "123456789",
            OrgName = "Test Organisation",
        };

        return new SigneeContext
        {
            TaskId = "task-id",
            Signee = isOrganisation ? organisationSignee : personSignee,
            SigneeState = new SigneeState { SignatureRequestSmsSent = hasSentSms ?? false },
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
        PersonSignee personSignee = new()
        {
            Party = new Party { },
            SocialSecurityNumber = "123456789",
            FullName = "Test Person",
        };
        OrganisationSignee organisationSignee = new()
        {
            OrgParty = new Party { },
            OrgNumber = "123456789",
            OrgName = "Test Organisation",
        };

        return new SigneeContext
        {
            TaskId = "task-id",
            Signee = isOrganisation ? organisationSignee : personSignee,
            SigneeState = new SigneeState { SignatureRequestEmailSent = hasSentEmail ?? false },
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
        };
    }
}
