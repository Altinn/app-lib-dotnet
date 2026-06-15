using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Helpers;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Tests.Features.Signing.Helpers;

public class SigningNotificationHelpers
{
    [Theory]
    [InlineData(NotificationChoice.None, "Default - Email")]
    [InlineData(NotificationChoice.Email, "Email")]
    [InlineData(NotificationChoice.Sms, "SMS")]
    [InlineData(NotificationChoice.SmsAndEmail, "SMS and Email")]
    [InlineData(NotificationChoice.SmsPreferred, "SMS preferred")]
    [InlineData(NotificationChoice.EmailPreferred, "Email preferred")]
    [InlineData((NotificationChoice)999, "Notification choice not set")]
    public void GetNotificationChoiceString_ShouldReturnCorrectString(
        NotificationChoice notificationChoice,
        string expected
    )
    {
        // Arrange & Act
        string result = SigningNotificationHelper.GetNotificationChoiceString(notificationChoice);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNotificationChoiceIfNotSet_ShouldReturnCorrectNotificationChoice()
    {
        // Arrange
        Notification notificationWithEmailAndSms = new()
        {
            Email = new Email { EmailAddress = "test@test.no" },
            Sms = new Sms { MobileNumber = "12345678" },
        };

        Notification notificationWithEmailOnly = new()
        {
            Email = new Email { EmailAddress = "test@test.no" },
            Sms = null,
        };
        Notification notificationWithSmsOnly = new()
        {
            Email = null,
            Sms = new Sms { MobileNumber = "12345678" },
        };

        Notification notificationWithNone = new() { Email = null, Sms = null };

        // Act & Assert
        Assert.Equal(
            NotificationChoice.SmsAndEmail,
            SigningNotificationHelper.GetNotificationChoiceIfNotSet(notificationWithEmailAndSms)
        );
        Assert.Equal(
            NotificationChoice.Email,
            SigningNotificationHelper.GetNotificationChoiceIfNotSet(notificationWithEmailOnly)
        );
        Assert.Equal(
            NotificationChoice.Sms,
            SigningNotificationHelper.GetNotificationChoiceIfNotSet(notificationWithSmsOnly)
        );
        Assert.Equal(
            NotificationChoice.None,
            SigningNotificationHelper.GetNotificationChoiceIfNotSet(notificationWithNone)
        );
    }

    [Fact]
    public void GetNotificationChoiceIfNotSet_InfersFromBlockPresence_NotPopulatedAddresses()
    {
        // Declaring a block (even without an explicit address) expresses intent to notify on that channel; the
        // contact is resolved from the registry for the default correspondence recipient.
        Notification emailAndSmsBlocks = new() { Email = new Email(), Sms = new Sms() };
        Notification emailBlockOnly = new() { Email = new Email() };
        Notification smsBlockOnly = new() { Sms = new Sms() };

        Assert.Equal(
            NotificationChoice.SmsAndEmail,
            SigningNotificationHelper.GetNotificationChoiceIfNotSet(emailAndSmsBlocks)
        );
        Assert.Equal(NotificationChoice.Email, SigningNotificationHelper.GetNotificationChoiceIfNotSet(emailBlockOnly));
        Assert.Equal(NotificationChoice.Sms, SigningNotificationHelper.GetNotificationChoiceIfNotSet(smsBlockOnly));
    }

    [Fact]
    public void CreateNotification_SmsAndEmail_WithBothOverrides_CreatesOneRecipientPerMethod()
    {
        // Arrange
        ContentWrapper cw = BuildContentWrapper(
            NotificationChoice.SmsAndEmail,
            new Notification
            {
                Email = new Email { EmailAddress = "test@test.no" },
                Sms = new Sms { MobileNumber = "12345678" },
            }
        );

        // Act
        CorrespondenceNotification? notification = SigningNotificationHelper.CreateNotification(cw);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(CorrespondenceNotificationChannel.EmailAndSms, notification.NotificationChannel);
        Assert.NotNull(notification.CustomRecipients);
        Assert.Equal(2, notification.CustomRecipients!.Count);

        Assert.Equal("test@test.no", notification.CustomRecipients[0].EmailAddress);
        Assert.Null(notification.CustomRecipients[0].MobileNumber);

        Assert.Equal("12345678", notification.CustomRecipients[1].MobileNumber);
        Assert.Null(notification.CustomRecipients[1].EmailAddress);
    }

    [Fact]
    public void CreateNotification_Email_WithOverrides_DoesNotLeakMobileIntoEmailNotification()
    {
        // Arrange - both contact methods are provided, but the choice is email only
        ContentWrapper cw = BuildContentWrapper(
            NotificationChoice.Email,
            new Notification
            {
                Email = new Email { EmailAddress = "test@test.no" },
                Sms = new Sms { MobileNumber = "12345678" },
            }
        );

        // Act
        CorrespondenceNotification? notification = SigningNotificationHelper.CreateNotification(cw);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(CorrespondenceNotificationChannel.Email, notification.NotificationChannel);
        Assert.NotNull(notification.CustomRecipients);
        CorrespondenceNotificationRecipient recipient = Assert.Single(notification.CustomRecipients!);
        Assert.Equal("test@test.no", recipient.EmailAddress);
        Assert.Null(recipient.MobileNumber);
    }

    [Fact]
    public void CreateNotification_WithoutContactOverride_DoesNotSetCustomRecipients()
    {
        // Arrange - no explicit contact overrides; delivery falls back to the correspondence recipient
        ContentWrapper cw = BuildContentWrapper(NotificationChoice.SmsAndEmail, notification: null);

        // Act
        CorrespondenceNotification? notification = SigningNotificationHelper.CreateNotification(cw);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(CorrespondenceNotificationChannel.EmailAndSms, notification.NotificationChannel);
        Assert.Null(notification.CustomRecipients);
    }

    [Fact]
    public void CreateNotification_None_DoesNotSetCustomRecipients()
    {
        // Arrange
        ContentWrapper cw = BuildContentWrapper(NotificationChoice.None, notification: null);

        // Act
        CorrespondenceNotification? notification = SigningNotificationHelper.CreateNotification(cw);

        // Assert
        Assert.NotNull(notification);
        Assert.Null(notification.CustomRecipients);
    }

    private static ContentWrapper BuildContentWrapper(NotificationChoice choice, Notification? notification)
    {
        return new ContentWrapper
        {
            CorrespondenceContent = new CorrespondenceContent
            {
                Title = "title",
                Language = LanguageCode<Iso6391>.Parse("nb"),
                Summary = "summary",
                Body = "body",
            },
            SendersReference = "senders-ref",
            NotificationChoice = choice,
            Notification = notification,
        };
    }
}
