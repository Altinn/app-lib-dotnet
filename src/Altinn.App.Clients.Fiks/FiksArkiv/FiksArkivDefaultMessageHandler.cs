using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
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
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;
    private readonly Telemetry? _telemetry;
    private readonly GeneralSettings _generalSettings;
    private readonly ITranslationService _translationService;
    private readonly TimeProvider _timeProvider;
    private readonly IAppModel _appModelResolver;

    private ApplicationMetadata? _applicationMetadataCache;

    public FiksArkivDefaultMessageHandler(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IOptions<FiksIOSettings> fiksIOSettings,
        IOptions<GeneralSettings> generalSettings,
        ITranslationService translationService,
        IAppMetadata appMetadata,
        IAppModel appModelResolver,
        IDataClient dataClient,
        IAuthenticationContext authenticationContext,
        IAltinnPartyClient altinnPartyClient,
        ILogger<FiksArkivDefaultMessageHandler> logger,
        IAltinnCdnClient altinnCdnClient,
        InstanceDataUnitOfWorkInitializer instanceDataUnitOfWorkInitializer,
        ILayoutEvaluatorStateInitializer layoutStateInitializer,
        IEmailNotificationClient emailNotificationClient,
        IHostEnvironment hostEnvironment,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        TimeProvider? timeProvider = null,
        Telemetry? telemetry = null
    )
    {
        _appMetadata = appMetadata;
        _appModelResolver = appModelResolver;
        _dataClient = dataClient;
        _altinnCdnClient = altinnCdnClient;
        _altinnPartyClient = altinnPartyClient;
        _authenticationContext = authenticationContext;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksIOSettings = fiksIOSettings.Value;
        _generalSettings = generalSettings.Value;
        _translationService = translationService;
        _instanceDataUnitOfWorkInitializer = instanceDataUnitOfWorkInitializer;
        _layoutStateInitializer = layoutStateInitializer;
        _emailNotificationClient = emailNotificationClient;
        _hostEnvironment = hostEnvironment;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _telemetry = telemetry;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
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

    private string GetCorrelationId(Instance instance) => instance.GetInstanceUrl(_generalSettings);

    public Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    )
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

        if (_fiksArkivSettings.Receipt is null)
            throw new FiksArkivConfigurationException(
                $"{nameof(FiksArkivSettings.Receipt)} configuration is required for default handler {GetType().Name}."
            );

        _fiksArkivSettings.Receipt.Validate(nameof(_fiksArkivSettings.Receipt), configuredDataTypes);

        _fiksArkivSettings.AutoSend?.ErrorHandling?.Validate();
        _fiksArkivSettings.Metadata?.Validate(configuredDataTypes, _appModelResolver);

        return Task.CompletedTask;
    }

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

    private async Task<IReadOnlyList<DeserializationResult>?> DeserializeContent(FiksIOReceivedMessage receivedMessage)
    {
        var payloads = await receivedMessage.Message.GetDecryptedPayloadStrings();
        if (payloads is null)
            return null;

        List<DeserializationResult> responses = [];
        foreach (var (filename, content) in payloads)
        {
            DeserializationResult? result = null;

            try
            {
                result = receivedMessage.Message.MessageType switch
                {
                    FiksArkivMeldingtype.ArkivmeldingOpprettKvittering => new DeserializationResult(
                        content,
                        content.DeserializeXml<ArkivmeldingKvittering>(),
                        null
                    ),
                    FiksArkivMeldingtype.Ikkefunnet => new DeserializationResult(
                        content,
                        null,
                        content.DeserializeXml<Ikkefunnet>()
                    ),
                    FiksArkivMeldingtype.Serverfeil => new DeserializationResult(
                        content,
                        null,
                        content.DeserializeXml<Serverfeil>()
                    ),
                    FiksArkivMeldingtype.Ugyldigforespørsel => new DeserializationResult(
                        content,
                        null,
                        content.DeserializeXml<Ugyldigforespoersel>()
                    ),
                    _ => null,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deserializing message content: {Exception}", e.Message);
            }

            responses.Add(result ?? new DeserializationResult(content, null, null));
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
