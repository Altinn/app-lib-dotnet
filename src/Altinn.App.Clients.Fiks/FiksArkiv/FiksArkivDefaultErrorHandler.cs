using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultErrorHandler : IFiksArkivErrorHandler
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly ILogger<FiksArkivDefaultErrorHandler> _logger;

    public FiksArkivDefaultErrorHandler(
        IEmailNotificationClient emailNotificationClient,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        ILogger<FiksArkivDefaultErrorHandler> logger
    )
    {
        _logger = logger;
        _emailNotificationClient = emailNotificationClient;
        _fiksArkivSettings = fiksArkivSettings.Value;
    }

    public async Task HandleError(Instance instance, FiksIOReceivedMessageArgs receivedMessage)
    {
        _logger.LogError("Full message details: {Melding}", receivedMessage.Message);
        string recipientEmailAddress = ValidateAndReturnEmailAddress();

        var result = await _emailNotificationClient.Order(
            new EmailNotification
            {
                Subject = $"Altinn: Fiks Arkiv feil i {instance.AppId}",
                Body =
                    $"Det har oppstått en feil ved sending av melding til Fiks Arkiv for Altinn app instans {instance.Id}.\n\nVidere undersøkelser må gjøres manuelt. Se logg for ytterligere detaljer.",
                Recipients = [new EmailRecipient(recipientEmailAddress)],
                SendersReference = instance.Id,
                RequestedSendTime = DateTime.UtcNow,
            },
            CancellationToken.None
        );

        _logger.LogInformation("Email order successfully submitted: {OrderId}", result.OrderId);
    }

    private string ValidateAndReturnEmailAddress()
    {
        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.ErrorNotificationEmailAddress))
            throw new Exception(
                $"FiksArkivSettings.ErrorNotificationEmailAddress is required for {nameof(FiksArkivDefaultErrorHandler)}, but has not been configured."
            );

        return _fiksArkivSettings.ErrorNotificationEmailAddress;
    }

    public void ValidateConfiguration()
    {
        ValidateAndReturnEmailAddress();
    }
}
