using System.Text.Json;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Process;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivReplyServiceTask : IReplyServiceTask
{
    private readonly ILogger<FiksArkivServiceTask> _logger;
    private readonly IFiksArkivHost _fiksArkivHost;
    private readonly FiksArkivSettings _fiksArkivSettings;

    public string Type => "fiksArkiv";

    public FiksArkivReplyServiceTask(
        IFiksArkivHost fiksArkivHost,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        ILogger<FiksArkivServiceTask> logger
    )
    {
        _fiksArkivHost = fiksArkivHost;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _logger = logger;
    }

    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        try
        {
            Instance instance = context.InstanceDataMutator.Instance;
            string taskId = instance.Process.CurrentTask.ElementId;

            _logger.LogInformation(
                "FiksArkivServiceTask is executing for instance {InstanceId} and task {TaskId}",
                instance.Id,
                taskId
            );

            var response = await _fiksArkivHost.GenerateAndSendMessage(
                taskId,
                context.InstanceDataMutator,
                FiksArkivConstants.MessageTypes.CreateArchiveRecord,
                context.CorrelationId
            );

            _logger.LogInformation(
                "FiksArkivServiceTask completed for instance {InstanceId} with response: {Response}",
                instance.Id,
                response
            );

            return ServiceTaskResult.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while executing FiksArkivServiceTask: {ErrorMessage}", e.Message);

            return _fiksArkivSettings.ErrorHandling?.MoveToNextTask is true
                ? ServiceTaskResult.FailedContinueProcessNext(_fiksArkivSettings.ErrorHandling?.GetActionOrDefault())
                : ServiceTaskResult.FailedAbortProcessNext();
        }
    }

    public Task<ServiceTaskResult> ProcessReply(ServiceTaskContext context, string payload)
    {
        var storedMessage =
            JsonSerializer.Deserialize<StoredFiksArkivMessage>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize stored Fiks Arkiv message.");

        _logger.LogInformation(
            "Processing stored Fiks Arkiv message {MessageId} of type {MessageType}",
            storedMessage.MessageId,
            storedMessage.MessageType
        );

        // Parse and deserialize payloads (replicates FiksArkivHost.DecryptAndDeserializePayloads)
        IReadOnlyList<FiksArkivReceivedMessagePayload>? payloads = storedMessage
            .Payloads?.Select(p => ParseMessagePayload(p.Filename, p.Content, storedMessage.MessageType))
            .ToList();

        // Determine if this is an error response (replicates FiksArkivHost.HandleReceivedMessage)
        bool isError =
            FiksIOConstants.IsErrorType(storedMessage.MessageType)
            || payloads?.OfType<FiksArkivReceivedMessagePayload.Error>().Any() is true;

        if (isError)
        {
            _logger.LogError(
                "Stored Fiks Arkiv message {MessageId} of type {MessageType} is an error response: {MessageContent}",
                storedMessage.MessageId,
                storedMessage.MessageType,
                payloads?.Select(x => x.Content) ?? ["Message contains no content."]
            );

            return Task.FromResult<ServiceTaskResult>(ServiceTaskResult.FailedContinueProcessNext("reject"));
        }

        _logger.LogInformation(
            "Stored Fiks Arkiv message {MessageId} of type {MessageType} is a successful response: {MessageContent}",
            storedMessage.MessageId,
            storedMessage.MessageType,
            payloads?.Select(x => x.Content) ?? ["Message contains no content."]
        );

        // Persist receipt on the instance (replicates FiksArkivHost lines 177-191)
        if (FiksIOConstants.IsReceiptType(storedMessage.MessageType))
        {
            if (payloads?.FirstOrDefault() is not FiksArkivReceivedMessagePayload.Receipt receipt)
            {
                _logger.LogWarning(
                    "No receipt payload found in stored Fiks message of type {ReceiptMessageType}. This is unexpected. Payloads were: {Payloads}",
                    FiksArkivConstants.MessageTypes.ArchiveRecordCreationReceipt,
                    payloads?.Select(x => x.Filename)
                );

                return Task.FromResult<ServiceTaskResult>(ServiceTaskResult.Success());
            }

            SaveArchiveReceipt(context.InstanceDataMutator, receipt);
        }

        return Task.FromResult<ServiceTaskResult>(ServiceTaskResult.Success());
    }

    private void SaveArchiveReceipt(IInstanceDataMutator mutator, FiksArkivReceivedMessagePayload.Receipt receipt)
    {
        if (_fiksArkivSettings.Receipt is null)
        {
            _logger.LogWarning("FiksArkivSettings.Receipt is not configured. Skipping receipt persistence.");
            return;
        }

        FiksArkivDataTypeSettings confirmationSettings = _fiksArkivSettings.Receipt.ConfirmationRecord;
        string dataTypeId = confirmationSettings.DataType;
        string filename = confirmationSettings.GetFilenameOrDefault();

        // Remove existing receipt data elements (replicates FiksArkivHost.DeleteExistingDataElements)
        var existingElements = mutator
            .Instance.Data.Where(de =>
                de.DataType.Equals(dataTypeId, StringComparison.OrdinalIgnoreCase) && de.Filename == filename
            )
            .ToList();

        foreach (DataElement existingElement in existingElements)
        {
            _logger.LogInformation(
                "Removing existing receipt data element {DataElementId} ({DataType}/{Filename})",
                existingElement.Id,
                dataTypeId,
                filename
            );
            mutator.RemoveDataElement(existingElement);
        }

        // Serialize the receipt and add as a new binary data element
        ReadOnlyMemory<byte> receiptBytes = receipt.Details.SerializeXml();

        mutator.AddBinaryDataElement(dataTypeId, "application/xml", filename, receiptBytes);

        _logger.LogInformation(
            "Added archive receipt as binary data element ({DataType}/{Filename})",
            dataTypeId,
            filename
        );
    }

    private FiksArkivReceivedMessagePayload ParseMessagePayload(string filename, string payload, string messageType)
    {
        try
        {
            object? deserializedPayload = messageType switch
            {
                FiksArkivMeldingtype.ArkivmeldingOpprettKvittering => payload.DeserializeXml<ArkivmeldingKvittering>()
                    ?? throw new InvalidOperationException(
                        $"Error deserializing {nameof(ArkivmeldingKvittering)} data"
                    ),
                FiksArkivMeldingtype.Ikkefunnet => payload.DeserializeXml<Ikkefunnet>()
                    ?? throw new InvalidOperationException($"Error deserializing {nameof(Ikkefunnet)} data"),
                FiksArkivMeldingtype.Serverfeil => payload.DeserializeXml<Serverfeil>()
                    ?? throw new InvalidOperationException($"Error deserializing {nameof(Serverfeil)} data"),
                FiksArkivMeldingtype.Ugyldigforespørsel => payload.DeserializeXml<Ugyldigforespoersel>()
                    ?? throw new InvalidOperationException($"Error deserializing {nameof(Ugyldigforespoersel)} data"),
                _ => null,
            };

            return FiksArkivReceivedMessagePayload.Create(filename, payload, deserializedPayload);
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            _logger.LogError(e, "Error deserializing XML data: {Exception}", e.Message);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, "{Exception}: {Content}", e.Message, payload);
        }

        return new FiksArkivReceivedMessagePayload.Unknown(filename, payload);
    }
}
