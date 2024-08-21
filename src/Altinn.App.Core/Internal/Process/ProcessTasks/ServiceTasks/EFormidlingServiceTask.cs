using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal interface IEFormidlingServiceTask : IServiceTask { }

/// <summary>
/// Service task that sends eFormidling shipment, if EFormidling is enabled in config.
/// </summary>
public class EFormidlingServiceTask : IEFormidlingServiceTask
{
    private readonly ILogger<EFormidlingServiceTask> _logger;
    private readonly IInstanceClient _instanceClient;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly IOptions<AppSettings>? _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFormidlingServiceTask"/> class.
    /// </summary>
    public EFormidlingServiceTask(
        ILogger<EFormidlingServiceTask> logger,
        IInstanceClient instanceClient,
        IEFormidlingService? eFormidlingService = null,
        IOptions<AppSettings>? appSettings = null
    )
    {
        _logger = logger;
        _instanceClient = instanceClient;
        _eFormidlingService = eFormidlingService;
        _appSettings = appSettings;
    }

    /// <inheritdoc />
    public string Type => "eFormidling";

    /// <inheritdoc/>
    public async Task Execute(string taskId, Instance instance)
    {
        //TODO: Keep only if we want to be able to disable eFormidling in specific environments, as was possible in the previous implementation?
        if (_appSettings?.Value.EnableEFormidling is false)
        {
            _logger.LogWarning(
                "EFormidling is not enabled in appsettings.json. No eFormidling shipment will be sent, but the service task will be completed."
            );
            return;
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
    }

    /// <inheritdoc/>
    public Task Start(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task End(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Abandon(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }
}
