using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Telemetry.NotifySigneesConst;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningNotificationService : ISigningNotificationService
{
    private readonly ILogger<SigningNotificationService> _logger;
    private readonly ISmsNotificationClient? _smsNotificationClient;
    private readonly IEmailNotificationClient? _emailNotificationClient;
    private readonly Telemetry? _telemetry;

    private record NotificationDefaults
    {
        internal required string SmsBody { get; set; }
        internal required string EmailBody { get; set; }
        internal required string EmailSubject { get; set; }
    }

    private readonly NotificationDefaults _defaults = new()
    {
        SmsBody = "Du har mottatt en oppgave til signering. Du finner oppgaven i innboksen i Altinn.",
        EmailBody = "Du har mottatt en oppgave til signering. Du finner oppgaven i innboksen i Altinn.",
        EmailSubject = "Oppgave til signering i Altinn",
    };

    public SigningNotificationService(
        ILogger<SigningNotificationService> logger,
        ISmsNotificationClient? smsNotificationClient = null,
        IEmailNotificationClient? emailNotificationClient = null,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _smsNotificationClient = smsNotificationClient;
        _emailNotificationClient = emailNotificationClient;
        _telemetry = telemetry;
    }

    public async Task<List<SigneeContext>> NotifySignatureTask(
        List<SigneeContext> signeeContexts,
        CancellationToken? ct = null
    )
    {
        using var activity = _telemetry?.StartNotifySigneesActivity();
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
                        _logger.LogError(errorMessage);
                    }

                    state.SignatureRequestSmsSent = success;
                    state.SignatureRequestSmsNotSentReason = success ? null : errorMessage;
                    _telemetry?.RecordNotifySignees(NotifySigneesResult.Success);
                }

                if (state.SignatureRequestEmailSent is false && notification?.Email is not null)
                {
                    (bool success, string? errorMessage) = await TrySendEmail(notification.Email, ct);

                    if (success is false)
                    {
                        _logger.LogError(errorMessage);
                        _telemetry?.RecordNotifySignees(NotifySigneesResult.Error);
                    }

                    state.SignatureRequestEmailSent = success;
                    state.SignatureRequestEmailNotSentReason = success ? null : errorMessage;
                }
            }
            catch
            {
                _telemetry?.RecordNotifySignees(NotifySigneesResult.Error);
            }
        }

        return signeeContexts;
    }

    private async Task<(bool, string? errorMessage)> TrySendSms(Sms sms, CancellationToken? ct = null)
    {
        if (_smsNotificationClient is null)
        {
            return (false, "No implementation of ISmsNotificationClient registered. Unable to send notification.");
        }

        if (sms.MobileNumber is null)
        {
            return (false, "No mobile number provided. Unable to send SMS notification.");
        }

        var notification = new SmsNotification()
        {
            Recipients = [new SmsRecipient(sms.MobileNumber)],
            Body = sms.Body ?? _defaults.SmsBody,
            SenderNumber = "", // Default SMS sender number is used by setting the value to an empty string. This is set in the altinn-notification repository to be "Altinn".
            SendersReference = sms.Reference,
        };

        try
        {
            await _smsNotificationClient.Order(notification, ct ?? new CancellationToken());
            return (true, null);
        }
        catch (SmsNotificationException ex)
        {
            _logger.LogError(ex.Message, ex);
            return (false, "Failed to send SMS notification: " + ex.Message);
        }
    }

    private async Task<(bool, string?)> TrySendEmail(Email email, CancellationToken? ct = null)
    {
        if (_emailNotificationClient is null)
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
            Subject = email.Subject ?? _defaults.EmailSubject,
            Body = email.Body ?? _defaults.EmailBody,
            SendersReference = email.Reference,
        };

        try
        {
            await _emailNotificationClient.Order(notification, ct ?? new CancellationToken());
            return (true, null);
        }
        catch (SmsNotificationException ex)
        {
            _logger.LogError(ex.Message, ex);
            return (false, "Failed to send Email notification: " + ex.Message);
        }
    }
}
