using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Future;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly ISmsNotificationClient _smsNotificationClient;
    private readonly INotificationOrderClient _notificationOrderClient;
    private readonly NotificationTextHelper _textHelper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailNotificationClient emailNotificationClient,
        ISmsNotificationClient smsNotificationClient,
        INotificationOrderClient notificationOrderClient,
        NotificationTextHelper textHelper,
        ILogger<NotificationService> logger
    )
    {
        _emailNotificationClient = emailNotificationClient;
        _smsNotificationClient = smsNotificationClient;
        _notificationOrderClient = notificationOrderClient;
        _textHelper = textHelper;
        _logger = logger;
    }

    public async Task<List<NotificationReference>> NotifyInstanceOwner(
        string language,
        Instance instance,
        EmailConfig? emailOverride,
        SmsConfig? smsOverride,
        CancellationToken ct
    )
    {
        List<NotificationReference> notificationReferences = [];

        InstanceOwner instanceOwner =
            instance.InstanceOwner
            ?? throw new InvalidOperationException(
                "Instance owner must be set on instance to notify instance owner"
            );

        if (string.IsNullOrEmpty(instanceOwner.ExternalIdentifier) is false)
        {
            return await HandleSelfIdentifiedUser(language, instance, emailOverride, smsOverride, instanceOwner, ct);
        }

        if (emailOverride is not null && emailOverride.SendEmail)
        {
            (string subject, string body) = await _textHelper.GetEmailText(language, emailOverride);

            EmailRecipient instanceOwnerRecipient = GetInstanceOwnerEmailRecipient(instanceOwner);
            EmailNotification emailNotification = new()
            {
                Subject = subject,
                Body = body,
                Recipients = [instanceOwnerRecipient],
                SendersReference = $"instance-{instance.Id}-email",
            };

            notificationReferences.Add(await OrderEmail(emailNotification, ct));
        }

        if (smsOverride is not null && smsOverride.SendSms)
        {
            string body = await _textHelper.GetSmsBody(language, smsOverride);

            SmsRecipient instanceOwnerRecipient = GetInstanceOwnerSmsRecipient(instanceOwner);
            SmsNotification smsNotification = new()
            {
                SenderNumber = smsOverride.SenderNumber,
                Body = body,
                Recipients = [instanceOwnerRecipient],
                SendersReference = $"instance-{instance.Id}-sms",
            };

            notificationReferences.Add(await OrderSms(smsNotification, ct));
        }

        return notificationReferences;
    }

    private async Task<List<NotificationReference>> HandleSelfIdentifiedUser(string language, Instance instance, EmailConfig? emailOverride, SmsConfig? smsOverride, InstanceOwner instanceOwner, CancellationToken ct)
    {
        List<NotificationReference> notificationReferences = [];
        if (emailOverride is not null && emailOverride.SendEmail)
        {
            (string subject, string body) = await _textHelper.GetEmailText(language, emailOverride);

            var request = new NotificationOrderRequest
            {
                IdempotencyId = $"instance-{instance.Id}-email",
                SendersReference = $"instance-{instance.Id}-email",
                Recipient = new NotificationRecipient
                {
                    RecipientSelfIdentifiedUser = new RecipientSelfIdentifiedUser
                    {
                        ExternalIdentity = instanceOwner.ExternalIdentifier,
                        ChannelSchema = NotificationChannel.Email,
                        EmailSettings = new EmailSendingOptions
                        {
                            Subject = subject,
                            Body = body,
                        },
                    },
                },
            };

            NotificationOrderResponse response = await _notificationOrderClient.Order(request, ct);

            notificationReferences.Add(new NotificationReference(request.SendersReference, response.Notification.ShipmentId.ToString()));
        }

        if (smsOverride is not null && smsOverride.SendSms)
        {
            _logger.LogWarning(
                "SMS notifications are not supported for self-identified users. SMS will not be sent for instance {InstanceId}.",
                instance.Id
            );
        }

        return notificationReferences;
    }

    public async Task<List<NotificationReference>> ProcessNotificationOrders(
        string language,
        List<EmailNotification> emailNotifications,
        List<SmsNotification> smsNotifications,
        CancellationToken ct
    )
    {
        var notificationReferences = new List<NotificationReference>();
        foreach (EmailNotification emailNotification in emailNotifications)
        {
            notificationReferences.Add(await OrderEmail(emailNotification, ct));
        }

        foreach (SmsNotification smsNotification in smsNotifications)
        {
            notificationReferences.Add(await OrderSms(smsNotification, ct));
        }

        return notificationReferences;
    }

    private static EmailRecipient GetInstanceOwnerEmailRecipient(InstanceOwner instanceOwner)
    {
        if (string.IsNullOrEmpty(instanceOwner.OrganisationNumber) is false)
            return new EmailRecipient(OrganizationNumber: instanceOwner.OrganisationNumber);

        if (string.IsNullOrEmpty(instanceOwner.PersonNumber) is false)
            return new EmailRecipient(NationalIdentityNumber: instanceOwner.PersonNumber);

        // We have already handled self identified users.

        throw new InvalidOperationException(
            $"Instance owner with party id {instanceOwner.PartyId} has neither an organisation number nor a person number and cannot be sent email notifications"
        );
    }

    private static SmsRecipient GetInstanceOwnerSmsRecipient(InstanceOwner instanceOwner)
    {
        if (string.IsNullOrEmpty(instanceOwner.OrganisationNumber) is false)
            return new SmsRecipient(OrganisationNumber: instanceOwner.OrganisationNumber);

        if (string.IsNullOrEmpty(instanceOwner.PersonNumber) is false)
            return new SmsRecipient(NationalIdentityNumber: instanceOwner.PersonNumber);

        // We have already handled self identified users.

        throw new InvalidOperationException(
            $"Instance owner with party id {instanceOwner.PartyId} has neither an organisation number nor a person number and cannot be sent sms notifications"
        );
    }

    private async Task<NotificationReference> OrderEmail(EmailNotification emailNotification, CancellationToken ct)
    {
        EmailOrderResponse response = await _emailNotificationClient.Order(emailNotification, ct);
        return new NotificationReference(emailNotification.SendersReference, response.OrderId);
    }

    private async Task<NotificationReference> OrderSms(SmsNotification smsNotification, CancellationToken ct)
    {
        SmsOrderResponse response = await _smsNotificationClient.Order(smsNotification, ct);
        return new NotificationReference(smsNotification.SendersReference, response.OrderId);
    }
}
