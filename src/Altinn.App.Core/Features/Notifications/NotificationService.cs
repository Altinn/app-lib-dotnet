using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly ISmsNotificationClient _smsNotificationClient;
    private readonly ITranslationService _translationService;

    public NotificationService(
        IEmailNotificationClient emailNotificationClient,
        ISmsNotificationClient smsNotificationClient,
        ITranslationService translationService
    )
    {
        _emailNotificationClient = emailNotificationClient;
        _smsNotificationClient = smsNotificationClient;
        _translationService = translationService;
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

        InstanceOwner instanceOwner = instance.InstanceOwner ?? throw new InvalidOperationException("Instance owner must be set on instance to notify instance owner");

        if (emailOverride is not null && emailOverride.SendEmail)
        {
            string defaultSubjectTextResourceId = BackendTextResource.EmailDefaultSubject;
            string defaultBodyTextResourceId = BackendTextResource.EmailDefaultBody;
            string subject = await GetTextResource(
                language,
                defaultSubjectTextResourceId,
                emailOverride.SubjectTextResource
            );
            string body = await GetTextResource(language, defaultBodyTextResourceId, emailOverride.BodyTextResource);

            EmailRecipient instanceOwnerRecipient = GetInstanceOwnerEmailRecipient(instanceOwner);
            string sendersReference = $"instance-{instance.Id}-email";

            EmailNotification emailNotification = new()
            {
                Subject = subject,
                Body = body,
                Recipients = [instanceOwnerRecipient],
                SendersReference = sendersReference,
            };

            NotificationReference notificationReference = await OrderEmail(emailNotification, ct);
            notificationReferences.Add(notificationReference);
        }

        if (smsOverride is not null && smsOverride.SendSms)
        {
            string defaultBodyTextResourceId = BackendTextResource.SmsDefaultBody;
            string body = await GetTextResource(language, defaultBodyTextResourceId, smsOverride.BodyTextResource);

            SmsRecipient instanceOwnerRecipient = GetInstanceOwnerSmsRecipient(instanceOwner);
            string sendersReference = $"instance-{instance.Id}-sms";

            SmsNotification smsNotification = new()
            {
                SenderNumber = smsOverride.SenderNumber,
                Body = body,
                Recipients = [instanceOwnerRecipient],
                SendersReference = sendersReference,
            };

            NotificationReference notificationReference = await OrderSms(smsNotification, ct);
            notificationReferences.Add(notificationReference);
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

        //TODO: handle intanceOwner Externalid - parse email out of the URN, when storage.interfaces is updated

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

        // TODO: handle intanceOwner Externalid - parse mobile number out of the URN, when storage.interfaces is updated

        throw new InvalidOperationException(
            $"Instance owner with party id {instanceOwner.PartyId} has neither an organisation number nor a person number and cannot be sent sms notifications"
        );
    }

    private async Task<string> GetTextResource(
        string language,
        string defaultTextResourceId,
        string? textResourceId = null
    )
    {
        string? translatedText =
            await _translationService.TranslateTextKey(language, textResourceId ?? defaultTextResourceId)
            ?? throw new InvalidOperationException(
                $"Default text resource '{defaultTextResourceId}' could not be found for language '{language}'"
            );
        return translatedText;
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
