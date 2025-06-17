using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Enums;
using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Helpers;

internal sealed class SigningNotificationHelper
{
    internal static string GetNotificationChoiceString(NotificationChoice notificationChoice)
    {
        return notificationChoice switch
        {
            NotificationChoice.None => "No notification",
            NotificationChoice.Email => "Email",
            NotificationChoice.Sms => "SMS",
            NotificationChoice.SmsAndEmail => "SMS and Email",
            NotificationChoice.SmsPreferred => "SMS preferred",
            NotificationChoice.EmailPreferred => "Email preferred",
            _ => "Default - Email",
        };
    }

    internal static CorrespondenceNotification? CreateNotification(ContentWrapper contextWrapper)
    {
        NotificationChoice? notificationChoice = contextWrapper.NotificationChoice;
        var emailSubject = contextWrapper.EmailSubject;
        var emailBody = contextWrapper.EmailBody;
        var smsBody = contextWrapper.SmsBody;
        var sendersReference = contextWrapper.SendersReference;

        var emailAddress = contextWrapper.Notification?.Email?.EmailAddress;
        var mobileNumber = contextWrapper.Notification?.Sms?.MobileNumber;

        var recipient = contextWrapper.Recipient;
        var reminderNotification = contextWrapper.ReminderNotification;
        var reminderEmailBody = contextWrapper.ReminderEmailBody;
        var reminderEmailSubject = contextWrapper.ReminderEmailSubject;
        var reminderSmsBody = contextWrapper.ReminderSmsBody;

        return notificationChoice switch
        {
            NotificationChoice.Email => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Email)
                .WithEmailSubject(emailSubject)
                .WithEmailBody(emailBody)
                .WithSendersReference(sendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(recipient)
                        .Build()
                )
                .WithSendReminder(reminderNotification is not null)
                .WithReminderEmailBody(reminderEmailBody)
                .WithReminderEmailSubject(reminderEmailSubject)
                .WithReminderSmsBody(reminderSmsBody)
                .Build(),
            NotificationChoice.Sms => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Sms)
                .WithSmsBody(smsBody)
                .WithSendersReference(sendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(recipient)
                        .Build()
                )
                .WithSendReminder(reminderNotification is not null)
                .WithReminderEmailBody(reminderEmailBody)
                .WithReminderEmailSubject(reminderEmailSubject)
                .WithReminderSmsBody(reminderSmsBody)
                .Build(),
            NotificationChoice.SmsAndEmail => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.EmailPreferred)
                .WithSmsBody(smsBody)
                .WithEmailSubject(emailSubject)
                .WithEmailBody(emailBody)
                .WithSendersReference(sendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(recipient)
                        .Build()
                )
                .WithSendReminder(reminderNotification is not null)
                .WithReminderEmailBody(reminderEmailBody)
                .WithReminderEmailSubject(reminderEmailSubject)
                .WithReminderSmsBody(reminderSmsBody)
                .Build(),
            NotificationChoice.SmsPreferred => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.SmsPreferred)
                .WithSmsBody(smsBody)
                .WithEmailSubject(emailSubject)
                .WithEmailBody(emailBody)
                .WithSendersReference(sendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithMobileNumber(mobileNumber)
                        .WithOrganisationOrPersonIdentifier(recipient)
                        .Build()
                )
                .WithSendReminder(reminderNotification is not null)
                .WithReminderEmailBody(reminderEmailBody)
                .WithReminderEmailSubject(reminderEmailSubject)
                .WithReminderSmsBody(reminderSmsBody)
                .Build(),
            NotificationChoice.EmailPreferred => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.CustomMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.EmailPreferred)
                .WithSmsBody(smsBody)
                .WithEmailSubject(emailSubject)
                .WithEmailBody(emailBody)
                .WithSendersReference(sendersReference)
                .WithRecipientOverride(
                    CorrespondenceNotificationOverrideBuilder
                        .Create()
                        .WithEmailAddress(emailAddress)
                        .WithOrganisationOrPersonIdentifier(recipient)
                        .Build()
                )
                .WithSendReminder(reminderNotification is not null)
                .WithReminderEmailBody(reminderEmailBody)
                .WithReminderEmailSubject(reminderEmailSubject)
                .WithReminderSmsBody(reminderSmsBody)
                .Build(),
            NotificationChoice.None => CorrespondenceNotificationBuilder
                .Create()
                .WithNotificationTemplate(CorrespondenceNotificationTemplate.GenericAltinnMessage)
                .WithNotificationChannel(CorrespondenceNotificationChannel.Email)
                .WithEmailSubject(emailSubject)
                .WithEmailBody(emailBody)
                .WithSendersReference(sendersReference)
                .Build(),
            _ => null,
        };
    }
}
