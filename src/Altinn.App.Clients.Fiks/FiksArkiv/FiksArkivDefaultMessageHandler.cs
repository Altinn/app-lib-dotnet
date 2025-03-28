using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.ASiC_E;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FiksResult = Altinn.App.Core.Features.Telemetry.Fiks.FiksResult;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed partial class FiksArkivDefaultMessageHandler : IFiksArkivMessageHandler
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly FiksIOSettings _fiksIOSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly ILogger<FiksArkivDefaultMessageHandler> _logger;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly IAltinnCdnClient _altinnCdnClient;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly ILayoutEvaluatorStateInitializer _layoutStateInitializer;
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Telemetry? _telemetry;
    private readonly GeneralSettings _generalSettings;

    private ApplicationMetadata? _applicationMetadataCache;

    public FiksArkivDefaultMessageHandler(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IOptions<FiksIOSettings> fiksIOSettings,
        IOptions<GeneralSettings> generalSettings,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IAuthenticationContext authenticationContext,
        IAltinnPartyClient altinnPartyClient,
        ILogger<FiksArkivDefaultMessageHandler> logger,
        IAltinnCdnClient altinnCdnClient,
        InstanceDataUnitOfWorkInitializer instanceDataUnitOfWorkInitializer,
        ILayoutEvaluatorStateInitializer layoutStateInitializer,
        IEmailNotificationClient emailNotificationClient,
        IHostEnvironment hostEnvironment,
        Telemetry? telemetry = null
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _altinnCdnClient = altinnCdnClient;
        _altinnPartyClient = altinnPartyClient;
        _authenticationContext = authenticationContext;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksIOSettings = fiksIOSettings.Value;
        _generalSettings = generalSettings.Value;
        _logger = logger;
        _instanceDataUnitOfWorkInitializer = instanceDataUnitOfWorkInitializer;
        _layoutStateInitializer = layoutStateInitializer;
        _emailNotificationClient = emailNotificationClient;
        _hostEnvironment = hostEnvironment;
        _telemetry = telemetry;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        if (IsEnabledForTask(taskId) is false)
            throw new FiksArkivException($"Fiks Arkiv error: Auto-send is not enabled for this task: {taskId}");

        var recipient = await GetRecipient(instance);
        var instanceId = new InstanceIdentifier(instance.Id);
        var messagePayloads = await GenerateMessagePayloads(instance, recipient);

        return new FiksIOMessageRequest(
            Recipient: recipient.AccountId,
            MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(2),
            Payload: messagePayloads,
            CorrelationId: GetCorrelationId(instance)
        );
    }

    public async Task HandleReceivedMessage(Instance instance, FiksIOReceivedMessage receivedMessage)
    {
        IReadOnlyList<DeserializationResult>? deserializedContent = await DeserializeContent(receivedMessage);
        bool isError = receivedMessage.IsErrorResponse || deserializedContent?.Any(x => x.IsErrorResponse) is true;

        _telemetry?.RecordFiksMessageReceived(isError ? FiksResult.Error : FiksResult.Success);

        await (
            isError
                ? HandleError(instance, receivedMessage, deserializedContent)
                : HandleSuccess(instance, receivedMessage, deserializedContent)
        );
    }

    private string GetCorrelationId(Instance instance)
    {
        var baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance));
        return $"{baseUrl}instances/{instance.Id}";
    }

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
    {
        _fiksArkivSettings.Validate(configuredDataTypes, configuredProcessTasks);
        return Task.CompletedTask;
    }

    private bool IsEnabledForTask(string taskId) => _fiksArkivSettings.AutoSend?.AfterTaskId == taskId;

    private async Task<ApplicationMetadata> GetApplicationMetadata()
    {
        _applicationMetadataCache ??= await _appMetadata.GetApplicationMetadata();
        return _applicationMetadataCache;
    }

    private static T VerifiedNotNull<T>(T? value)
    {
        if (value is null)
            throw new FiksArkivException($"Value of type {typeof(T).Name} is unexpectedly null.");

        return value;
    }

    private async Task<IReadOnlyList<string>> GetDecryptedPayloads(FiksIOReceivedMessage receivedMessage)
    {
        try
        {
            List<string> payloads = [];
            AsiceReader asiceReader = new();
            using var asiceReadModel = asiceReader.Read(await receivedMessage.Message.DecryptedStream);

            foreach (var verifyReadEntry in asiceReadModel.Entries)
            {
                await using var entryStream = verifyReadEntry.OpenStream();
                using var reader = new StreamReader(entryStream);
                payloads.Add(await reader.ReadToEndAsync());
            }

            return payloads;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error reading decrypted message content: {Exception}", e.Message);
        }

        return [];
    }

    private async Task<IReadOnlyList<DeserializationResult>?> DeserializeContent(FiksIOReceivedMessage receivedMessage)
    {
        string messageType = receivedMessage.Message.MessageType;
        if (receivedMessage.Message.HasPayload is false)
            return null;

        var payloads = await GetDecryptedPayloads(receivedMessage);
        if (payloads.Any() is false)
            return null;

        List<DeserializationResult> responses = [];
        foreach (var payload in payloads)
        {
            DeserializationResult? result = null;

            try
            {
                result = messageType switch
                {
                    FiksArkivMeldingtype.ArkivmeldingOpprettKvittering => new DeserializationResult(
                        payload,
                        payload.DeserializeXml<ArkivmeldingKvittering>(),
                        null
                    ),
                    FiksArkivMeldingtype.Ikkefunnet => new DeserializationResult(
                        payload,
                        null,
                        payload.DeserializeXml<Ikkefunnet>()
                    ),
                    FiksArkivMeldingtype.Serverfeil => new DeserializationResult(
                        payload,
                        null,
                        payload.DeserializeXml<Serverfeil>()
                    ),
                    FiksArkivMeldingtype.Ugyldigforespørsel => new DeserializationResult(
                        payload,
                        null,
                        payload.DeserializeXml<Ugyldigforespoersel>()
                    ),
                    _ => null,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deserializing message content: {Exception}", e.Message);
            }

            responses.Add(result ?? new DeserializationResult(payload, null, null));
        }

        return responses;
    }

    private sealed record DeserializationResult
    {
        public string StringResult { get; }
        public ArchiveReceipt? ReceiptResult { get; }
        public FeilmeldingBase? ErrorResult { get; }

        public bool IsErrorResponse => ErrorResult is not null || ReceiptResult?.IsErrorResponse is true;

        public DeserializationResult(
            string stringResult,
            ArkivmeldingKvittering? receiptResult,
            FeilmeldingBase? errorResult
        )
        {
            StringResult = stringResult;
            ReceiptResult = receiptResult is null ? null : new ArchiveReceipt(receiptResult);
            ErrorResult = errorResult;
        }

        public bool DeserializationSuccess => ReceiptResult is not null || ErrorResult is not null;

        public sealed record ArchiveReceipt
        {
            public SaksmappeKvittering? CaseFileReceipt { get; }
            public JournalpostKvittering? JournalEntryReceipt { get; }
            public FeilmeldingBase? CaseFileError { get; }
            public FeilmeldingBase? JournalEntryError { get; }

            public bool IsErrorResponse => CaseFileError is not null || JournalEntryError is not null;

            public ArchiveReceipt(ArkivmeldingKvittering arkivmeldingKvittering)
            {
                CaseFileReceipt = arkivmeldingKvittering.MappeKvittering as SaksmappeKvittering;
                JournalEntryReceipt = arkivmeldingKvittering.RegistreringKvittering as JournalpostKvittering;
                CaseFileError = arkivmeldingKvittering.MappeFeilet;
                JournalEntryError = arkivmeldingKvittering.RegistreringFeilet;
            }
        }
    }
}
