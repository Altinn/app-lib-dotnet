using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed partial class FiksArkivDefaultMessageHandler
{
    private const string ReceiptMessageType = FiksArkivMeldingtype.ArkivmeldingOpprettKvittering;

    private async Task HandleSuccess(
        Instance instance,
        FiksIOReceivedMessage receivedMessage,
        IReadOnlyList<DeserializationResult>? deserializedContent
    )
    {
        await Task.CompletedTask;

        _logger.LogInformation(
            "Received message {MessageType}:{MessageId} is a successful response: {MessageContent}",
            receivedMessage.Message.MessageType,
            receivedMessage.Message.MessageId,
            deserializedContent?.Select(x => x.StringResult) ?? ["Message contains no content."]
        );

        if (receivedMessage.Message.MessageType != ReceiptMessageType)
        {
            _logger.LogInformation(
                "We are only interested in {TargetMessageType} messages. Skipping further processing for message of type {MessageType}.",
                ReceiptMessageType,
                receivedMessage.Message.MessageType
            );
            return;
        }

        if (deserializedContent?.Count > 1)
            _logger.LogWarning(
                "Message contains multiple responses. This is unexpected and possibly warrants further investigation."
            );

        // TODO: Persist receipt IDs in instance storage
        // TODO: Present persisted data in custom receipt page?
        DeserializationResult? messageContent = deserializedContent?.FirstOrDefault();
        var caseFileReceipt = messageContent?.ReceiptResult?.CaseFileReceipt;
        var journalReceipt = messageContent?.ReceiptResult?.JournalEntryReceipt;

        // TODO: Send /complete notification?

        return;
    }
}
