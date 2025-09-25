using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.Platform.Storage.Interface.Models;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="EFormidlingServiceTask"/> class.
    /// </summary>
    public EFormidlingServiceTask(
        ILogger<EFormidlingServiceTask> logger,
        IEFormidlingService? eFormidlingService = null,
        IOptions<AppSettings>? appSettings = null
    )
    {
        _logger = logger;
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

        if (_appSettings?.Value.EnableEFormidling is false)
        {
            _logger.LogWarning(
                "EFormidling has been added as a service task in the BPMN process definition but is not enabled in appsettings.json. No eFormidling shipment will be sent, but the service task will be completed."
            );
            return new ServiceTaskSuccessResult();
        }

        if (_eFormidlingService is null)
        {
            throw new ProcessException(
                $"No implementation of {nameof(IEFormidlingService)} has been added to the DI container."
            );
        }

        try
        {
            _logger.LogDebug("Calling eFormidlingService for eFormidling Service Task {TaskId}.", taskId);
            await _eFormidlingService.SendEFormidlingShipment(instance);
            _logger.LogDebug("Successfully called eFormidlingService for eFormidling Service Task {TaskId}.", taskId);

            return ServiceTaskResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occured while executing {Type} Service Task on taskId {TaskId}.",
                Type,
                taskId
            );

            return ServiceTaskResult.Failed();
        }
    }
}
