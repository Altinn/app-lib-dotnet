using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Features.Signing.Services;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
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
    private readonly ISigneeContextsManager _signeeContextsManager;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;

    public SigningProcessTask(
        ISigningService signingService,
        IProcessReader processReader,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        ISigneeContextsManager signeeContextsManager,
        InstanceDataUnitOfWorkInitializer instanceDataUnitOfWorkInitializer
    )
    {
        _signingService = signingService;
        _processReader = processReader;
        _appMetadata = appMetadata;
        _hostEnvironment = hostEnvironment;
        _signeeContextsManager = signeeContextsManager;
        _instanceDataUnitOfWorkInitializer = instanceDataUnitOfWorkInitializer;
    }

    public string Type => "signing";

    /// <inheritdoc/>
    public async Task Start(string taskId, Instance instance)
    {
        using var cts = new CancellationTokenSource();

        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        ValidateSigningConfiguration(appMetadata, signatureConfiguration);

        InstanceDataUnitOfWork cachedDataMutator = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            taskId,
            null
        );

        if (
            signatureConfiguration.SigneeProviderId is not null
            && signatureConfiguration.SigneeStatesDataTypeId is not null
        )
        {
            await InitialiseRuntimeDelegatedSigning(cachedDataMutator, signatureConfiguration, cts.Token);
        }

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);
    }

    /// <inheritdoc/>
    public async Task End(string taskId, Instance instance)
    {
        // PDF feature disabled for now. Can be uncommented if someone strongly need it quickly, but we'd rather offer the possibility to generate a PDF of the signing task using a service task in the not too distant future.
        // Comment can be deleted if we support it through a service task in the future.
        //
        // AltinnSignatureConfiguration? signatureConfiguration = _processReader
        //     .GetAltinnTaskExtension(taskId)
        //     ?.SignatureConfiguration;
        //
        // string? signingPdfDataType = signatureConfiguration?.SigningPdfDataType;
        //
        // if (signingPdfDataType is not null)
        // {
        //     using Stream pdfStream = await _pdfService.GeneratePdf(instance, taskId, false, CancellationToken.None);
        //
        //     await _dataClient.InsertBinaryData(
        //         instance.Id,
        //         signingPdfDataType,
        //         PdfContentType,
        //         signingPdfDataType + ".pdf",
        //         pdfStream,
        //         taskId
        //     );
        // }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Abandon(string taskId, Instance instance)
    {
        using var cts = new CancellationTokenSource();

        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);

        var cachedDataMutator = await _instanceDataUnitOfWorkInitializer.Init(instance, taskId, null);

        await _signingService.AbortRuntimeDelegatedSigning(cachedDataMutator, signatureConfiguration, cts.Token);

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);
    }

    private async Task InitialiseRuntimeDelegatedSigning(
        IInstanceDataMutator cachedDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    )
    {
        List<SigneeContext> signeeContexts = await _signeeContextsManager.GenerateSigneeContexts(
            cachedDataMutator,
            signatureConfiguration,
            ct
        );

        await _signingService.InitializeSignees(cachedDataMutator, signeeContexts, signatureConfiguration, ct);
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
