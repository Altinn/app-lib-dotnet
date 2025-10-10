using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Models;
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

        ArgumentNullException.ThrowIfNull(instance);
        if (_fiksArkivSettings.AutoSend?.ErrorHandling is null)
        {
            _logger.LogInformation("Error handling is disabled, skipping further processing.");
            return;
        }

        InstanceIdentifier instanceIdentifier = new(instance);
        ApplicationMetadata appMetadata = await GetApplicationMetadata();

        // Email notifications
        if (_fiksArkivSettings.AutoSend.ErrorHandling.SendEmailNotifications is true)
        {
            List<EmailRecipient> recipientEmailAddresses = VerifiedNotNull(
                    _fiksArkivSettings.AutoSend.ErrorHandling.EmailNotificationRecipients
                )
                .Select(x => new EmailRecipient(x))
                .ToList();

            EmailOrderResponse result = await _emailNotificationClient.Order(
                new EmailNotification
                {
                    Subject = $"Altinn: Fiks Arkiv feil i {appMetadata.AppIdentifier}",
                    Body =
                        $"Det har oppstått en feil ved sending av melding til Fiks Arkiv for Altinn app instans {instanceIdentifier}.\n\nVidere undersøkelser må gjøres manuelt. Se logg for ytterligere detaljer.",
                    Recipients = recipientEmailAddresses,
                    SendersReference = instanceIdentifier.ToString(),
                    RequestedSendTime = _timeProvider.GetUtcNow().DateTime,
                },
                CancellationToken.None
            );

            _logger.LogInformation("Email order successfully submitted: {OrderId}", result.OrderId);
        }

        // Move the instance process forward if configured
        if (_fiksArkivSettings.AutoSend?.ErrorHandling?.MoveToNextTask is true)
            await _fiksArkivInstanceClient.ProcessMoveNext(
                instanceIdentifier,
                _fiksArkivSettings.AutoSend?.ErrorHandling?.Action
            );
    }
}
