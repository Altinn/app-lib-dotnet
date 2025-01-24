using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    private readonly IPdfService _pdfService;
    private readonly ILogger<SigningProcessTask> _logger;

    public SigningProcessTask(
        ISigningService signingService,
        IProcessReader processReader,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IDataClient dataClient,
        IInstanceClient instanceClient,
        ModelSerializationService modelSerialization,
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
        _pdfService = pdfService;
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

            _signingService.RemoveSigningData(cachedDataMutator, signatureConfiguration);

            List<SigneeContext> signeeContexts = await _signingService.GenerateSigneeContexts(
                cachedDataMutator,
                signatureConfiguration,
                cts.Token
            );

            await _signingService.InitialiseSignees(
                taskId,
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

        string? signingPdfDataType = signatureConfiguration?.SigningPdfDataType;

        if (signingPdfDataType is not null)
        {
            Stream pdfStream = await _pdfService.GeneratePdf(instance, taskId, false, CancellationToken.None);

            await _dataClient.InsertBinaryData(
                instance.Id,
                signingPdfDataType,
                PdfContentType,
                signingPdfDataType + ".pdf",
                pdfStream,
                taskId
            );
        }
    }

    /// <inheritdoc/>
    public async Task Abandon(string taskId, Instance instance)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        _signingService.RemoveSigningData(cachedDataMutator, signatureConfiguration);

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);
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
