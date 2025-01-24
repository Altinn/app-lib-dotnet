using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Represents the process task responsible for signing.
/// </summary>
internal sealed class SigningProcessTask : IProcessTask
{
    private readonly ISigningService _signingService;
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IDataClient _dataClient;
    private readonly IInstanceClient _instanceClient;
    private readonly ModelSerializationService _modelSerialization;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPdfService _pdfService;
    private readonly UserHelper _userHelper;
    private readonly ILogger<SigningProcessTask> _logger;

    public SigningProcessTask(
        ISigningService signingService,
        IProcessReader processReader,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IDataClient dataClient,
        IInstanceClient instanceClient,
        ModelSerializationService modelSerialization,
        IHttpContextAccessor httpContextAccessor,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClientClient,
        IOptions<GeneralSettings> settings,
        IPdfService pdfService,
        ILogger<SigningProcessTask> logger
    )
    {
        _signingService = signingService;
        _processReader = processReader;
        _appMetadata = appMetadata;
        _hostEnvironment = hostEnvironment;
        _dataClient = dataClient;
        _instanceClient = instanceClient;
        _modelSerialization = modelSerialization;
        _httpContextAccessor = httpContextAccessor;
        _pdfService = pdfService;
        _userHelper = new UserHelper(profileClient, altinnPartyClientClient, settings);
        _logger = logger;
    }

    public string Type => "signing";

    private const string PdfContentType = "application/pdf";

    /// <inheritdoc/>
    public async Task Start(string taskId, Instance instance)
    {
        var cts = new CancellationTokenSource();

        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        _logger.LogInformation($"Starting signing task for instance {instance.Id}");
        _logger.LogInformation($"Signature configuration: {signatureConfiguration.SigneeStatesDataTypeId}");
        _logger.LogInformation($"App metadata: {appMetadata}");

        if (_hostEnvironment.IsDevelopment())
        {
            AllowedContributorsHelper.EnsureDataTypeIsAppOwned(
                appMetadata,
                signatureConfiguration.SigneeStatesDataTypeId
            );
        }

        if (signatureConfiguration.SigneeProviderId is null != signatureConfiguration.SigneeStatesDataTypeId is null)
        {
            throw new ApplicationConfigException(
                $"Both {nameof(signatureConfiguration.SigneeProviderId)} and {nameof(signatureConfiguration.SigneeStatesDataTypeId)} must either be set together, or left unset. These properties are required to enable delegated signing."
            );
        }

        if (
            signatureConfiguration.SigneeProviderId is not null
            && signatureConfiguration.SigneeStatesDataTypeId is not null
        )
        {
            var cachedDataMutator = new InstanceDataUnitOfWork(
                instance,
                _dataClient,
                _instanceClient,
                appMetadata,
                _modelSerialization
            );

            List<SigneeContext> signeeContexts = await _signingService.InitializeSignees(
                cachedDataMutator,
                signatureConfiguration,
                cts.Token
            );

            if (signeeContexts.Count == 0)
            {
                _logger.LogInformation("No signees returned from signee initialization.");
                return;
            }

            UserContext userContext = await _userHelper.GetUserContext(
                _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.")
            );

            await _signingService.ProcessSignees(
                taskId,
                userContext.UserParty,
                cachedDataMutator,
                signeeContexts,
                signatureConfiguration,
                cts.Token
            );

            DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);

            await cachedDataMutator.UpdateInstanceData(changes);
            await cachedDataMutator.SaveChanges(changes);
        }
    }

    /// <inheritdoc/>
    /// <remarks> Generates a PDF if the signature configuration specifies a signature data type. </remarks>
    public async Task End(string taskId, Instance instance)
    {
        AltinnSignatureConfiguration? signatureConfiguration = _processReader
            .GetAltinnTaskExtension(taskId)
            ?.SignatureConfiguration;

        string? signatureDataType = signatureConfiguration?.SignatureDataType;

        if (signatureDataType is not null)
        {
            Stream pdfStream = await _pdfService.GeneratePdf(instance, taskId, false, CancellationToken.None);

            await _dataClient.InsertBinaryData(
                instance.Id,
                signatureDataType,
                PdfContentType,
                signatureDataType + ".pdf" ?? "signature_" + taskId + ".pdf",
                pdfStream,
                taskId
            );
        }
    }

    /// <inheritdoc/>
    public async Task Abandon(string taskId, Instance instance)
    {
        await Task.CompletedTask;
    }

    private AltinnSignatureConfiguration GetAltinnSignatureConfiguration(string taskId)
    {
        AltinnSignatureConfiguration? signatureConfiguration = _processReader
            .GetAltinnTaskExtension(taskId)
            ?.SignatureConfiguration;

        if (signatureConfiguration == null)
        {
            throw new ApplicationConfigException(
                "SignatureConfig is missing in the signature process task configuration."
            );
        }

        return signatureConfiguration;
    }
}
