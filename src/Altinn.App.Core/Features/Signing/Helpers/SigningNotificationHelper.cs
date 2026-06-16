using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Helpers;

internal sealed class SigningNotificationHelper
{
    internal static string GetNotificationChoiceString(NotificationChoice notificationChoice)
    {
        return notificationChoice switch
        {
            NotificationChoice.None => "Default - Email",
            NotificationChoice.Email => "Email",
            NotificationChoice.Sms => "SMS",
            NotificationChoice.SmsAndEmail => "SMS and Email",
            NotificationChoice.SmsPreferred => "SMS preferred",
            NotificationChoice.EmailPreferred => "Email preferred",
            _ => "Notification choice not set",
        };
    }

    /// <summary>
    /// Determines the notification choice based on the provided notification object.
    /// This is to keep backwards compatibility.
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    internal static NotificationChoice GetNotificationChoiceIfNotSet(Notification? notification)
    {
        if (
            notification?.Email is not null
            && notification.Email.EmailAddress is not null
            && notification.Sms is not null
            && notification.Sms.MobileNumber is not null
        )
        {
            return NotificationChoice.SmsAndEmail;
        }

        if (notification?.Email is not null && notification.Email.EmailAddress is not null)
        {
            return NotificationChoice.Email;
        }

        if (notification?.Sms is not null && notification.Sms.MobileNumber is not null)
        {
            return NotificationChoice.Sms;
        }

        return NotificationChoice.None;
    }

    internal static CorrespondenceNotification? CreateNotification(ContentWrapper cw)
    {
        string? emailAddress = cw.Notification?.Email?.EmailAddress;
        string? mobileNumber = cw.Notification?.Sms?.MobileNumber;

        return cw.NotificationChoice switch
        {
            NotificationChoice.Email => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Email)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithSigneeRecipientOverrides(emailAddress, null)
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.Sms => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Sms)
                .WithSmsBody(cw.SmsBody)
                .WithSendersReference(cw.SendersReference)
                .WithSigneeRecipientOverrides(null, mobileNumber)
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.SmsAndEmail => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.EmailAndSms)
                .WithSmsBody(cw.SmsBody)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithSigneeRecipientOverrides(emailAddress, mobileNumber)
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.SmsPreferred => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.SmsPreferred)
                .WithSmsBody(cw.SmsBody)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithSigneeRecipientOverrides(emailAddress, mobileNumber)
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.EmailPreferred => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.EmailPreferred)
                .WithSmsBody(cw.SmsBody)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithSigneeRecipientOverrides(emailAddress, mobileNumber)
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.None => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.GenericAltinnMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Email)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .Build(),
            _ => null,
        };
    }
}

/// <summary>
/// Signing-specific helpers for configuring custom notification recipients on a <see cref="CorrespondenceNotificationBuilder"/>.
/// </summary>
internal static class SigningNotificationBuilderExtensions
{
    /// <summary>
    /// Adds the signee's notification recipient overrides (one single-identifier entry per channel, as required by the
    /// Correspondence API), overriding the contact information registered in KRR. When no addresses are supplied, the
    /// notification falls back to the registered contact information of the correspondence recipient.
    /// </summary>
    internal static ICorrespondenceNotificationBuilder WithSigneeRecipientOverrides(
        this ICorrespondenceNotificationBuilder builder,
        string? emailAddress,
        string? mobileNumber
    )
    {
        var recipients = new List<CorrespondenceNotificationRecipient>(2);
        if (!string.IsNullOrEmpty(emailAddress))
        {
            recipients.Add(new CorrespondenceNotificationRecipient { EmailAddress = emailAddress });
        }
        if (!string.IsNullOrEmpty(mobileNumber))
        {
            recipients.Add(new CorrespondenceNotificationRecipient { MobileNumber = mobileNumber });
        }

        return builder.WithRecipientOverridesIfConfigured(recipients);
    }
}
