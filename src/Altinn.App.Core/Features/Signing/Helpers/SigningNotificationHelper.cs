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

#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
        var opt = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            };
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances

        Console.WriteLine($"=== ContentWrapper Debug ===");
        Console.WriteLine($"Recipient: {cw.Recipient}");
        Console.WriteLine($"NotificationChoice: {cw.NotificationChoice}");
        Console.WriteLine($"Email Address: {cw.Notification?.Email?.EmailAddress}");
        Console.WriteLine($"Mobile Number: {cw.Notification?.Sms?.MobileNumber}");
        Console.WriteLine($"Full ContentWrapper: {System.Text.Json.JsonSerializer.Serialize(cw, opt)}");

        return cw.NotificationChoice switch
        {
            NotificationChoice.Email => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Email)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(cw.Recipient)
                        .Build()
                )
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
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(cw.Recipient)
                        .Build()
                )
                .WithSendReminder(cw.ReminderNotification is not null)
                .WithReminderEmailBody(cw.ReminderEmailBody)
                .WithReminderEmailSubject(cw.ReminderEmailSubject)
                .WithReminderSmsBody(cw.ReminderSmsBody)
                .Build(),
            NotificationChoice.SmsAndEmail => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.EmailPreferred)
                .WithSmsBody(cw.SmsBody)
                .WithEmailSubject(cw.EmailSubject)
                .WithEmailBody(cw.EmailBody)
                .WithSendersReference(cw.SendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(cw.Recipient)
                        .Build()
                )
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
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(cw.Recipient)
                        .Build()
                )
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
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithOrganisationOrPersonIdentifier(cw.Recipient)
                        .Build()
                )
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
