using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
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

    public SigningProcessTask(
        ISigningService signingService,
        IProcessReader processReader,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment
    )
    {
        _signingService = signingService;
        _processReader = processReader;
        _appMetadata = appMetadata;
        _hostEnvironment = hostEnvironment;
    }

    public string Type => "signing";

    /// <inheritdoc/>
    public async Task Start(string taskId, Instance instance)
    {
        var cts = new CancellationTokenSource();

        AltinnSignatureConfiguration signatureConfiguration = GetAltinnSignatureConfiguration(taskId);

        if (_hostEnvironment.IsDevelopment())
        {
            ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
            AllowedContributorsHelper.EnsureDataTypeIsAppOwned(appMetadata, signatureConfiguration.SignatureDataType);
        }

        List<SigneeContext> signeeContexts = await _signingService.InitializeSignees(
            instance,
            signatureConfiguration,
            cts.Token
        );

        await _signingService.ProcessSignees(instance, signeeContexts, cts.Token);
    }

    /// <inheritdoc/>
    public async Task End(string taskId, Instance instance)
    {
        await Task.CompletedTask;
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
