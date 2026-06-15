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
    /// <remarks>
    /// The choice is inferred from which notification blocks the app declared (<see cref="Notification.Email"/> /
    /// <see cref="Notification.Sms"/>), not from whether explicit contact addresses are populated. Declaring a block
    /// expresses intent to notify on that channel; the contact itself is resolved from the registry for the default
    /// correspondence recipient when no explicit override is provided.
    /// </remarks>
    /// <param name="notification"></param>
    /// <returns></returns>
    internal static NotificationChoice GetNotificationChoiceIfNotSet(Notification? notification)
    {
        bool hasEmail = notification?.Email is not null;
        bool hasSms = notification?.Sms is not null;

        if (hasEmail && hasSms)
        {
            return NotificationChoice.SmsAndEmail;
        }

        if (hasEmail)
        {
            return NotificationChoice.Email;
        }

        if (hasSms)
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
                .WithCustomRecipientsIfConfigured(BuildCustomRecipients(emailAddress, mobileNumber: null))
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
                .WithCustomRecipientsIfConfigured(BuildCustomRecipients(emailAddress: null, mobileNumber))
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
                .WithCustomRecipientsIfConfigured(BuildCustomRecipients(emailAddress, mobileNumber))
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
                .WithCustomRecipientsIfConfigured(BuildCustomRecipients(emailAddress: null, mobileNumber))
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
                .WithCustomRecipientsIfConfigured(BuildCustomRecipients(emailAddress, mobileNumber: null))
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

    /// <summary>
    /// Builds one custom notification recipient per explicitly provided contact method. The Correspondence API
    /// requires each custom recipient to carry exactly one identifier, so email and mobile contacts become separate
    /// recipients. When no explicit contact is provided, the notification falls back to the correspondence recipient
    /// (resolved via the contact and reservation registry) and the notification channel governs delivery.
    /// </summary>
    private static List<CorrespondenceNotificationRecipient> BuildCustomRecipients(
        string? emailAddress,
        string? mobileNumber
    )
    {
        var recipients = new List<CorrespondenceNotificationRecipient>();

        if (emailAddress is not null)
        {
            recipients.Add(new CorrespondenceNotificationRecipient { EmailAddress = emailAddress });
        }

        if (mobileNumber is not null)
        {
            recipients.Add(new CorrespondenceNotificationRecipient { MobileNumber = mobileNumber });
        }

        return recipients;
    }
}
