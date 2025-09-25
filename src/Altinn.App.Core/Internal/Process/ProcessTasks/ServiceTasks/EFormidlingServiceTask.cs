using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal interface IEFormidlingServiceTask : IServiceTask { }

/// <summary>
/// Service task that sends eFormidling shipment, if EFormidling is enabled in config.
/// </summary>
internal sealed class EFormidlingServiceTask : IEFormidlingServiceTask
{
    private readonly ILogger<EFormidlingServiceTask> _logger;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly IOptions<AppSettings>? _appSettings;
    private readonly IProcessReader _processReader;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFormidlingServiceTask"/> class.
    /// </summary>
    public EFormidlingServiceTask(
        ILogger<EFormidlingServiceTask> logger,
        IProcessReader processReader,
        IHostEnvironment hostEnvironment,
        IEFormidlingService? eFormidlingService = null,
        IOptions<AppSettings>? appSettings = null
    )
    {
        _logger = logger;
        _processReader = processReader;
        _hostEnvironment = hostEnvironment;
        _eFormidlingService = eFormidlingService;
        _appSettings = appSettings;
    }

    /// <inheritdoc />
    public string Type => "eFormidling";

    /// <inheritdoc/>
    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        string taskId = context.InstanceDataMutator.Instance.Process.CurrentTask.ElementId;
        Instance instance = context.InstanceDataMutator.Instance;

        // Check BPMN configuration first, then fall back to appsettings for backward compatibility
        if (!IsEFormidlingEnabled(taskId))
        {
            _logger.LogInformation(
                "EFormidling is disabled for task {TaskId}. No eFormidling shipment will be sent, but the service task will be completed.",
                taskId
            );
            return ServiceTaskResult.Success();
        }

        if (_eFormidlingService is null)
        {
            throw new ProcessException(
                $"No implementation of {nameof(IEFormidlingService)} has been added to the DI container."
            );
        }

        _logger.LogDebug("Calling eFormidlingService for eFormidling Service Task {TaskId}.", taskId);
        await _eFormidlingService.SendEFormidlingShipment(instance);
        _logger.LogDebug("Successfully called eFormidlingService for eFormidling Service Task {TaskId}.", taskId);

        return ServiceTaskResult.Success();
    }

    private bool IsEFormidlingEnabled(string taskId)
    {
        AltinnTaskExtension? altinnTaskExtension = _processReader.GetAltinnTaskExtension(taskId);
        AltinnEFormidlingConfiguration? eFormidlingConfiguration = altinnTaskExtension?.EFormidlingConfiguration;

        // If no BPMN configuration is specified, default to enabled
        if (eFormidlingConfiguration?.Enabled.Count is 0 or null)
        {
            _logger.LogDebug(
                "No eFormidling configuration found in BPMN for task {TaskId}. Defaulting to enabled. Add <altinn:eFormidlingConfig><altinn:enabled env=\"staging\">false</altinn:enabled></altinn:eFormidlingConfig> to disable for specific environments.",
                taskId
            );
            return true;
        }

        // Use environment-aware BPMN configuration
        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
        AltinnEnvironmentConfig? enabledConfig = AltinnTaskExtension.GetConfigForEnvironment(
            env,
            eFormidlingConfiguration.Enabled
        );

        if (enabledConfig?.Value is null)
        {
            _logger.LogWarning(
                "EFormidling configuration is present in BPMN but no matching environment configuration found for environment '{Environment}'. EFormidling will be disabled.",
                env
            );
            return false;
        }

        return bool.TryParse(enabledConfig.Value, out bool enabled) && enabled;
    }
}
