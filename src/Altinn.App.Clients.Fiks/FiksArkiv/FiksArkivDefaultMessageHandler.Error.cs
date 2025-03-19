using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed partial class FiksArkivDefaultMessageHandler
{
    private async Task HandleError(
        Instance instance,
        FiksIOReceivedMessage receivedMessage,
        IReadOnlyList<DeserializationResult>? deserializedContent
    )
    {
        _logger.LogError(
            "Received message {MessageType}:{MessageId} is an error response: {MessageContent}",
            receivedMessage.Message.MessageType,
            receivedMessage.Message.MessageId,
            deserializedContent?.Select(x => x.StringResult) ?? ["Message contains no content."]
        );

        // TODO: TEMP -- remove this
        if (instance is null)
            return;

        if (_fiksArkivSettings.ErrorHandling?.SendEmailNotifications is not true)
        {
            _logger.LogInformation("Error handling is disabled, skipping email notification.");
            return;
        }

        IReadOnlyList<EmailRecipient> recipientEmailAddresses = VerifiedNotNull(
                _fiksArkivSettings.ErrorHandling.EmailNotificationRecipients
            )
            .Select(x => new EmailRecipient(x))
            .ToList();

        EmailOrderResponse result = await _emailNotificationClient.Order(
            new EmailNotification
            {
                Subject = $"Altinn: Fiks Arkiv feil i {instance.AppId}",
                Body =
                    $"Det har oppstått en feil ved sending av melding til Fiks Arkiv for Altinn app instans {instance.Id}.\n\nVidere undersøkelser må gjøres manuelt. Se logg for ytterligere detaljer.",
                Recipients = recipientEmailAddresses,
                SendersReference = instance.Id,
                RequestedSendTime = DateTime.UtcNow,
            },
            CancellationToken.None
        );

        _logger.LogInformation("Email order successfully submitted: {OrderId}", result.OrderId);
    }
}
