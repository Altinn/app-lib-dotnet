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
    public async Task<List<SigneeContext>> NotifySignatureTask(
        List<SigneeContext> signeeContexts,
        CancellationToken? ct = null
    )
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            SigneeParty party = signeeContext.SigneeParty;

            try
            {
                Notification? notification = party.Notifications?.SignatureTaskReceived;

                if (state.SignatureRequestSmsSent is false)
                {
                    if (notification?.Sms is not null)
                    {
                        state.SignatureRequestSmsSent = await TrySendSms(notification.Sms.MobileNumber, ct);
                    }
                }

                if (state.SignatureRequestEmailSent is false)
                {
                    if (notification?.Email is not null)
                    {
                        state.SignatureRequestEmailSent = await TrySendEmail(
                            notification.Email?.EmailAddress ?? "HOW TO GET FROM REGISTRY",
                            ct
                        );
                    }
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }

        return signeeContexts;
    }

    private async Task<bool> TrySendSms(string recipientNr, CancellationToken? ct = null)
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
            await smsNotificationClient.Order(notification, ct ?? new CancellationToken());
            return true;
        }
        catch (SmsNotificationException ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private async Task<bool> TrySendEmail(string email, CancellationToken? ct = null)
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
