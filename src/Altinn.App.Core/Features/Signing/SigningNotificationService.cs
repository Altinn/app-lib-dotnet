using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models.Notifications.Sms;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningNotificationService(
    ILogger<SigningNotificationService> logger,
    ISmsNotificationClient? smsNotificationClient = null,
    IEmailNotificationClient? emailNotificationClient = null
) : ISigningNotificationService
{
    public async Task NotifySignees(List<SigneeContext> signeeContexts, CancellationToken ct)
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            SigneeConfig config = signeeContext.SigneeParty;
            try
            {
                if (state.IsNotified is false)
                {
                    if (config.Notification.ShouldSendSms)
                    {
                        state.IsNotified = await TrySendSms(config.Notification.MobileNumber, ct);
                    }

                    if (config.Notification.ShouldSendEmail)
                    {
                        state.IsNotified = await TrySendEmail(config.Notification.MobileNumber, ct);
                    }
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }
    }

    private async Task<bool> TrySendSms(string recipientNr, CancellationToken ct)
    {
        // await Task.CompletedTask;
        // throw new NotImplementedException();
        if (smsNotificationClient is null)
        {
            logger.LogWarning("No implementation of ISmsNotificationClient registered. Unable to send notification.");
            return false;
        }

        var notification = new SmsNotification()
        {
            Body = "",
            Recipients = [new SmsRecipient(recipientNr, "", "")],
            SenderNumber = "",
            SendersReference = ""
        };
        try
        {
            await smsNotificationClient.Order(notification, ct);
            return true;
        }
        catch (SmsNotificationException ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private async Task<bool> TrySendEmail(string email, CancellationToken ct)
    {
        if (emailNotificationClient is null)
        {
            logger.LogWarning("No implementation of IEmailNotificationClient registered. Unable to send notification.");
            return false;
        }
        await Task.CompletedTask;
        throw new NotImplementedException();
        //TODO: implement fully

        //     var notification = new EmailNotification
        //     {
        //         Body = "",
        //         Recipients = [new EmailRecipient("")],
        //         Subject = "",
        //         SendersReference = ""
        //     };
        //     await emailNotificationClient.Order(notification, ct);
    }
}
