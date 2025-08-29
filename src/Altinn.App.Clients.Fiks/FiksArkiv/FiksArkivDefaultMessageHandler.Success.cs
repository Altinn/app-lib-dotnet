using System.Text.Json;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
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

        // Process and store receipt object
        ArgumentNullException.ThrowIfNull(instance);
        DeserializationResult? messageContent = deserializedContent?.FirstOrDefault();
        SaksmappeKvittering? caseFileReceipt = messageContent?.ReceiptResult?.CaseFileReceipt;
        JournalpostKvittering? journalReceipt = messageContent?.ReceiptResult?.JournalEntryReceipt;
        InstanceIdentifier instanceIdentifier = new(instance);

        FiksArkivReceipt receipt = FiksArkivReceipt.Create(caseFileReceipt, journalReceipt);
        byte[] receiptBytes = JsonSerializer.SerializeToUtf8Bytes(receipt);
        _logger.LogInformation("Receipt data received from Fiks message: {Receipt}", receipt);

        FiksArkivDataTypeSettings receiptConfig = VerifiedNotNull(_fiksArkivSettings.Receipt);
        string filename = !string.IsNullOrWhiteSpace(receiptConfig.Filename)
            ? receiptConfig.Filename
            : $"{receiptConfig.DataType}.json";

        using (var memoryStream = new MemoryStream(receiptBytes))
        {
            await _fiksArkivInstanceClient.InsertBinaryData(
                instanceIdentifier,
                receiptConfig.DataType,
                "application/json",
                filename,
                memoryStream
            );
        }

        // Move the instance process forward if configured
        if (_fiksArkivSettings.AutoSend?.SuccessHandling?.MoveToNextTask is true)
            await _fiksArkivInstanceClient.ProcessMoveNext(
                instanceIdentifier,
                _fiksArkivSettings.AutoSend?.SuccessHandling?.Action
            );

        // Mark the instance as completed if configured
        if (_fiksArkivSettings.AutoSend?.SuccessHandling?.MarkInstanceComplete is true)
            await _fiksArkivInstanceClient.MarkInstanceComplete(instanceIdentifier);
    }
}
