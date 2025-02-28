using Altinn.App.Core.Features;
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

    public SigningProcessTask(
        ISigningService signingService,
        IProcessReader processReader,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IDataClient dataClient,
        IInstanceClient instanceClient,
        ModelSerializationService modelSerialization,
        IPdfService pdfService
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
    }

    public string Type => "signing";

    private const string PdfContentType = "application/pdf";

    /// <inheritdoc/>
    public async Task Start(string taskId, Instance instance)
    {
        using var cts = new CancellationTokenSource();

        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        ValidateSigningConfiguration(appMetadata, signatureConfiguration);

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        if (
            signatureConfiguration.SigneeProviderId is not null
            && signatureConfiguration.SigneeStatesDataTypeId is not null
        )
        {
            await InitialiseRuntimeDelegatedSigning(taskId, cachedDataMutator, signatureConfiguration, cts.Token);
        }

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);
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
        var cts = new CancellationTokenSource();

        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        await _signingService.AbortRuntimeDelegatedSigning(
            taskId,
            cachedDataMutator,
            signatureConfiguration,
            cts.Token
        );

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);
    }

    private async Task InitialiseRuntimeDelegatedSigning(
        string taskId,
        IInstanceDataMutator cachedDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        List<SigneeContext> signeeContexts = await _signingService.GenerateSigneeContexts(
            cachedDataMutator,
            signatureConfiguration,
            ct
        );

        await _signingService.InitialiseSignees(taskId, cachedDataMutator, signeeContexts, signatureConfiguration, ct);
    }

    private AltinnSignatureConfiguration GetAltinnSignatureConfiguration(string taskId)
    {
        AltinnSignatureConfiguration? signatureConfiguration = _processReader
            .GetAltinnTaskExtension(taskId)
            ?.SignatureConfiguration;

        if (signatureConfiguration is null)
        {
            throw new ApplicationConfigException(
                "SignatureConfig is missing in the signature process task configuration."
            );
        }

        return signatureConfiguration;
    }

    private void ValidateSigningConfiguration(
        ApplicationMetadata appMetadata,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        string? signaturesDataType = signatureConfiguration.SignatureDataType;
        string? signeeStatesDataTypeId = signatureConfiguration.SigneeStatesDataTypeId;
        string? signeeProviderId = signatureConfiguration.SigneeProviderId;

        if (signaturesDataType is null)
        {
            throw new ApplicationConfigException(
                $"The {nameof(signatureConfiguration.SignatureDataType)} property must be set in the signature configuration."
            );
        }

        // The signatures data type should be app owned, so that the end user can't manipulate the data. Tell the developer during development if this is not the case.
        if (_hostEnvironment.IsDevelopment())
        {
            AllowedContributorsHelper.EnsureDataTypeIsAppOwned(appMetadata, signaturesDataType);
        }

        if (signeeProviderId is null != signeeStatesDataTypeId is null)
        {
            throw new ApplicationConfigException(
                $"Both {nameof(signatureConfiguration.SigneeProviderId)} and {nameof(signatureConfiguration.SigneeStatesDataTypeId)} must either be set together, or left unset. These properties are required to enable delegation based signing."
            );
        }

        // The signee state data type should be app owned, so that the end user can't manipulate the data. Tell the developer during development if this is not the case.
        if (_hostEnvironment.IsDevelopment())
        {
            AllowedContributorsHelper.EnsureDataTypeIsAppOwned(appMetadata, signeeStatesDataTypeId);
        }
    }
}
