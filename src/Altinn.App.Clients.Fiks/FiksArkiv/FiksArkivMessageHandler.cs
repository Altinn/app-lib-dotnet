using System.Text.Json;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FiksResult = Altinn.App.Core.Features.Telemetry.Fiks.FiksResult;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivMessageHandler : IFiksArkivMessageHandler
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly ILogger<FiksArkivMessageHandler> _logger;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly Telemetry? _telemetry;
    private readonly IAppModel _appModelResolver;
    private readonly IFiksArkivConfigResolver _fiksArkivConfigResolver;
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;

    private IFiksArkivMessagePayloadGenerator _fiksArkivMessagePayloadGenerator =>
        _appImplementationFactory.GetRequired<IFiksArkivMessagePayloadGenerator>();
    private IFiksArkivMessageResponseHandler _fiksArkivMessageResponseHandler =>
        _appImplementationFactory.GetRequired<IFiksArkivMessageResponseHandler>();

    public FiksArkivMessageHandler(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IAppModel appModelResolver,
        ILogger<FiksArkivMessageHandler> logger,
        IFiksArkivConfigResolver fiksArkivConfigResolver,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        AppImplementationFactory appImplementationFactory,
        Telemetry? telemetry = null
    )
    {
        _appModelResolver = appModelResolver;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _fiksArkivConfigResolver = fiksArkivConfigResolver;
        _appImplementationFactory = appImplementationFactory;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        var instanceId = new InstanceIdentifier(instance.Id);
        var recipient = await _fiksArkivConfigResolver.GetRecipient(instance);
        var messagePayloads = await _fiksArkivMessagePayloadGenerator.GeneratePayload(taskId, instance, recipient);

        return new FiksIOMessageRequest(
            Recipient: recipient.AccountId,
            MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(2),
            Payload: messagePayloads,
            CorrelationId: _fiksArkivConfigResolver.GetCorrelationId(instance)
        );
    }

    public async Task HandleReceivedMessage(Instance instance, FiksIOReceivedMessage receivedMessage)
    {
        IReadOnlyList<FiksArkivReceivedMessagePayload>? payloads = await DeserializePayloads(receivedMessage);
        bool isError =
            receivedMessage.IsErrorResponse || payloads?.OfType<FiksArkivReceivedMessagePayload.Error>().Any() is true;

        _telemetry?.RecordFiksMessageReceived(isError ? FiksResult.Error : FiksResult.Success);

        await (
            isError
                ? _fiksArkivMessageResponseHandler.HandleError(instance, receivedMessage, payloads)
                : _fiksArkivMessageResponseHandler.HandleSuccess(instance, receivedMessage, payloads)
        );

        // Persist receipt on the instance
        if (!isError)
        {
            ArgumentNullException.ThrowIfNull(_fiksArkivSettings.Receipt);
            var receipt = payloads?.FirstOrDefault() as FiksArkivReceivedMessagePayload.Receipt;
            byte[] receiptBytes = JsonSerializer.SerializeToUtf8Bytes(receipt?.Details ?? FiksArkivReceipt.Empty());
            _logger.LogInformation("Writing receipt data from Fiks message: {Receipt}", receipt);

            await _fiksArkivInstanceClient.InsertBinaryData(
                new InstanceIdentifier(instance),
                _fiksArkivSettings.Receipt.ConfirmationRecord.DataType,
                "application/json",
                _fiksArkivSettings.Receipt.ConfirmationRecord.GetFilenameOrDefault(".json"),
                receiptBytes
            );
        }
    }

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        if (_fiksArkivSettings.Receipt is null)
            throw new FiksArkivConfigurationException(
                $"{nameof(FiksArkivSettings.Receipt)} configuration is required for default handler {GetType().Name}."
            );

        _fiksArkivSettings.Receipt.Validate(nameof(_fiksArkivSettings.Receipt), configuredDataTypes);

        if (_fiksArkivMessagePayloadGenerator is FiksArkivDefaultMessagePayloadGenerator)
        {
            if (_fiksArkivSettings.Recipient is null)
                throw new FiksArkivConfigurationException(
                    $"{nameof(FiksArkivSettings.Recipient)} configuration is required for default handler {GetType().Name}."
                );
            _fiksArkivSettings.Recipient.Validate(configuredDataTypes, _appModelResolver);

            if (_fiksArkivSettings.Documents is null)
                throw new FiksArkivConfigurationException(
                    $"{nameof(FiksArkivSettings.Documents)} configuration is required for default handler {GetType().Name}."
                );
            _fiksArkivSettings.Documents.Validate(configuredDataTypes);

            _fiksArkivSettings.Metadata?.Validate(configuredDataTypes, _appModelResolver);
        }

        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<FiksArkivReceivedMessagePayload>?> DeserializePayloads(
        FiksIOReceivedMessage receivedMessage
    )
    {
        var payloads = await receivedMessage.Message.GetDecryptedPayloads();
        return payloads
            ?.Select(x => ParseMessagePayload(x.Filename, x.Content, receivedMessage.Message.MessageType))
            .ToList();
    }

    private FiksArkivReceivedMessagePayload ParseMessagePayload(string filename, string payload, string messageType)
    {
        try
        {
            object? deserializedPayload = messageType switch
            {
                FiksArkivMeldingtype.ArkivmeldingOpprettKvittering => payload.DeserializeXml<ArkivmeldingKvittering>()
                    ?? throw new FiksArkivException($"Error deserializing {nameof(ArkivmeldingKvittering)} data"),
                FiksArkivMeldingtype.Ikkefunnet => payload.DeserializeXml<Ikkefunnet>()
                    ?? throw new FiksArkivException($"Error deserializing {nameof(Ikkefunnet)} data"),
                FiksArkivMeldingtype.Serverfeil => payload.DeserializeXml<Serverfeil>()
                    ?? throw new FiksArkivException($"Error deserializing {nameof(Serverfeil)} data"),
                FiksArkivMeldingtype.Ugyldigforespørsel => payload.DeserializeXml<Ugyldigforespoersel>()
                    ?? throw new FiksArkivException($"Error deserializing {nameof(Ugyldigforespoersel)} data"),
                _ => null,
            };

            return FiksArkivReceivedMessagePayload.Create(filename, payload, deserializedPayload);
        }
        catch (FiksArkivException e)
        {
            _logger.LogError(e, "{Exception}: {Content}", e.Message, payload);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing XML data: {Exception}", e.Message);
        }

        return new FiksArkivReceivedMessagePayload.Unknown(filename, payload);
    }
}
