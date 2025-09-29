using Altinn.App.Core.Constants;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;

namespace Altinn.App.Core.EFormidling.Implementation;

/// <summary>
/// Provides validated eFormidling configuration from various sources.
/// </summary>
public interface IEFormidlingConfigurationProvider
{
    /// <summary>
    /// Gets validated eFormidling configuration from ApplicationMetadata (legacy).
    /// </summary>
    /// <returns>Validated eFormidling configuration.</returns>
    Task<ValidAltinnEFormidlingConfiguration> GetLegacyConfiguration();

    /// <summary>
    /// Gets validated eFormidling configuration from BPMN task extension.
    /// </summary>
    /// <param name="taskId">The task ID to get configuration for.</param>
    /// <returns>Validated eFormidling configuration.</returns>
    Task<ValidAltinnEFormidlingConfiguration> GetBpmnConfiguration(string taskId);
}

/// <summary>
/// Provides eFormidling configuration from various sources (ApplicationMetadata or BPMN).
/// </summary>
internal sealed class EFormidlingConfigurationProvider : IEFormidlingConfigurationProvider
{
    private readonly IAppMetadata _appMetadata;
    private readonly IProcessReader _processReader;
    private readonly IHostEnvironment _hostEnvironment;

    public EFormidlingConfigurationProvider(
        IAppMetadata appMetadata,
        IProcessReader processReader,
        IHostEnvironment hostEnvironment
    )
    {
        _appMetadata = appMetadata;
        _processReader = processReader;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<ValidAltinnEFormidlingConfiguration> GetLegacyConfiguration()
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        EFormidlingContract? eFormidling = applicationMetadata.EFormidling;

        if (eFormidling is null)
        {
            throw new ApplicationConfigException($"No legacy eFormidling configuration found in application metadata.");
        }

        return new ValidAltinnEFormidlingConfiguration(
            eFormidling.Receiver,
            eFormidling.Process,
            eFormidling.Standard,
            eFormidling.TypeVersion,
            eFormidling.Type,
            eFormidling.SecurityLevel,
            eFormidling.DPFShipmentType,
            eFormidling.DataTypes?.ToList() ?? []
        );
    }

    public Task<ValidAltinnEFormidlingConfiguration> GetBpmnConfiguration(string taskId)
    {
        ArgumentNullException.ThrowIfNull(taskId);

        AltinnTaskExtension? taskExtension = _processReader.GetAltinnTaskExtension(taskId);
        AltinnEFormidlingConfiguration? eFormidlingConfig = taskExtension?.EFormidlingConfiguration;

        if (eFormidlingConfig is null)
            throw new ApplicationConfigException($"No eFormidling configuration found in BPMN for task {taskId}");

        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
        ValidAltinnEFormidlingConfiguration validConfig = eFormidlingConfig.Validate(env);

        return Task.FromResult(validConfig);
    }
}
