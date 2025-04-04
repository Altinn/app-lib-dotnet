using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Models;
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

        ArgumentNullException.ThrowIfNull(instance);

        if (deserializedContent?.Count > 1)
            _logger.LogWarning(
                "Message contains multiple responses. This is unexpected and possibly warrants further investigation."
            );

        DeserializationResult? messageContent = deserializedContent?.FirstOrDefault();
        var caseFileReceipt = messageContent?.ReceiptResult?.CaseFileReceipt;
        var journalReceipt = messageContent?.ReceiptResult?.JournalEntryReceipt;
        var instanceIdentifier = new InstanceIdentifier(instance);
        var appIdentifier = new AppIdentifier(instance);

        var receipt = FiksArkivReceipt.Create(caseFileReceipt, journalReceipt);
        _logger.LogInformation("Receipt data received from Fiks message: {Receipt}", receipt);

        // TODO: Store receipt data in storage
        // _fiksArkivSettings.Receipt.DataType

        if (_fiksArkivSettings.AutoSend?.AutoProgressToNextTask is true)
        {
            await _fiksArkivInstanceClient.ProcessMoveNext(appIdentifier, instanceIdentifier);
        }
    }
}
