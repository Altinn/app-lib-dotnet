using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningNotificationService(
    ILogger<SigningNotificationService> logger,
    ISmsNotificationClient? smsNotificationClient = null,
    IEmailNotificationClient? emailNotificationClient = null
) : ISigningNotificationService
{
    public async Task<List<SigneeContext>> NotifySignatureTask(
        List<SigneeContext> signeeContexts,
        CancellationToken? ct = null
    )
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            Models.Notifications? notifications =
                signeeContext.OrganisationSignee?.Notifications ?? signeeContext.PersonSignee?.Notifications;

            try
            {
                Notification? notification = notifications?.OnSignatureAccessRightsDelegated;

                if (state.SignatureRequestSmsSent is false && notification?.Sms is not null)
                {
                    (bool success, string? errorMessage) = await TrySendSms(notification.Sms, ct);

                    if (success is false)
                    {
                        logger.LogError(errorMessage);
                    }

                    state.SignatureRequestSmsSent = success;
                    state.SignatureRequestSmsNotSentReason = success ? null : errorMessage;
                }

                if (state.SignatureRequestEmailSent is false && notification?.Email is not null)
                {
                    (bool success, string? errorMessage) = await TrySendEmail(notification.Email, ct);

                    if (success is false)
                    {
                        logger.LogError(errorMessage);
                    }

                    state.SignatureRequestEmailSent = success;
                    state.SignatureRequestEmailNotSentReason = success ? null : errorMessage;
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }

        return signeeContexts;
    }

    private async Task<(bool, string? errorMessage)> TrySendSms(Sms sms, CancellationToken? ct = null)
    {
        if (smsNotificationClient is null)
        {
            return (false, "No implementation of ISmsNotificationClient registered. Unable to send notification.");
        }

        if (sms.MobileNumber is null)
        {
            return (false, "No mobile number provided. Unable to send SMS notification.");
        }

        var notification = new SmsNotification()
        {
            Recipients = [new SmsRecipient(sms.MobileNumber, "", "")], //TODO: What do we get for setting orgnr or nin here?
            Body = sms.Body ?? "", //TODO: Should we have defaults or should this be required?
            SenderNumber = "",
            SendersReference = "",
        };

        try
        {
            await smsNotificationClient.Order(notification, ct ?? new CancellationToken());
            return (true, null);
        }
        catch (SmsNotificationException ex)
        {
            logger.LogError(ex.Message, ex);
            return (false, "Failed to send SMS notification: " + ex.Message);
        }
    }

    private async Task<(bool, string?)> TrySendEmail(Email email, CancellationToken? ct = null)
    {
        if (emailNotificationClient is null)
        {
            return (false, "No implementation of IEmailNotificationClient registered. Unable to send notification.");
        }

        if (email.EmailAddress is null)
        {
            return (false, "No email address provided. Unable to send SMS notification.");
        }

        var notification = new EmailNotification()
        {
            Recipients = [new EmailRecipient(email.EmailAddress)],
            Subject = email.Subject ?? "", //TODO: Should we have defaults or should this be required?
            Body = email.Body ?? "", //TODO: Should we have defaults or should this be required?
            SendersReference = "",
        };

        try
        {
            await emailNotificationClient.Order(notification, ct ?? new CancellationToken());
            return (true, null);
        }
        catch (SmsNotificationException ex)
        {
            logger.LogError(ex.Message, ex);
            return (false, "Failed to send Email notification: " + ex.Message);
        }
    }
}
