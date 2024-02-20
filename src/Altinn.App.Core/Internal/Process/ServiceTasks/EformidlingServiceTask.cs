using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// Service task that sends eFormidling shipment, if EFormidling is enabled in config and EFormidling.SendAfterTaskId matches the current task.
/// </summary>
public class EformidlingServiceTask : IServiceTask
{
    private readonly ILogger<EformidlingServiceTask> _logger;
    private readonly IAppMetadata _appMetadata;
    private readonly IInstanceClient _instanceClient;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly IOptions<AppSettings>? _appSettings;

    /// <summary>
    /// Service task that sends eFormidling shipment, if EFormidling is enabled in config and EFormidling.SendAfterTaskId matches the current task.
    /// </summary>
    public EformidlingServiceTask(ILogger<EformidlingServiceTask> logger, IAppMetadata appMetadata, IInstanceClient instanceClient, IEFormidlingService? eFormidlingService = null, IOptions<AppSettings>? appSettings = null)
    {
        _logger = logger;
        _appMetadata = appMetadata;
        _instanceClient = instanceClient;
        _eFormidlingService = eFormidlingService;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Executes the service task.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Execute(string taskId, Instance instance)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        if (_appSettings?.Value?.EnableEFormidling == true && applicationMetadata.EFormidling?.SendAfterTaskId == taskId)
        {
            if (_eFormidlingService != null)
            {
                var updatedInstance = await _instanceClient.GetInstance(instance);
                await _eFormidlingService.SendEFormidlingShipment(updatedInstance);
            }
            else
            {
                _logger.LogError("EformidlingService is not configured. No eFormidling shipment will be sent.");
            }
        }
    }
}