using Altinn.App.Core.Features.Signing.Enums;
using Altinn.App.Core.Features.Signing.Helpers;
using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Tests.Features.Signing.Helpers;

public class SigningCorrespondenceHelperTests
{
    [Fact]
    public void GetNotificationChoice_EmailAndSms_ReturnsSmsAndEmail()
    {
        // Arrange
        var notification = new Notification
        {
            Email = new Email { EmailAddress = "test@tester.no" },
            Sms = new Sms { MobileNumber = "12345678" },
        };

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.SmsAndEmail, result);
    }

    [Fact]
    public void GetNotificationChoice_EmailOnly_ReturnsEmail()
    {
        // Arrange
        var notification = new Notification { Email = new Email { EmailAddress = "test@tester.no" } };

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.Email, result);
    }

    [Fact]
    public void GetNotificationChoice_SmsOnly_ReturnsSms()
    {
        // Arrange
        var notification = new Notification { Sms = new Sms { MobileNumber = "12345678" } };

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.Sms, result);
    }

    [Fact]
    public void GetNotificationChoice_NoNotification_ReturnsNone()
    {
        // Arrange
        var notification = new Notification();

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.None, result);
    }

    [Fact]
    public void GetNotificationChoice_NullNotification_ReturnsNone()
    {
        // Arrange
        Notification? notification = null;

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.None, result);
    }

    [Fact]
    public void GetNotificationChoice_SmsButNoMobileNumber_ReturnsNone()
    {
        // Arrange
        var notification = new Notification { Sms = new Sms { BodyTextResourceKey = "sms_body" } };

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.None, result);
    }

    [Fact]
    public void GetNotificationChoice_EmailButNoEmailAddress_ReturnsNone()
    {
        // Arrange
        var notification = new Notification { Email = new Email { BodyTextResourceKey = "email_body" } };

        // Act
        var result = SigningCorrespondenceHelper.GetNotificationChoice(notification);

        // Assert
        Assert.Equal(NotificationChoice.None, result);
    }
}
