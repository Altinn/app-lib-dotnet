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
public class EformidlingServiceTask(ILogger<EformidlingServiceTask> logger, IAppMetadata appMetadata, IInstanceClient instanceClient, IEFormidlingService? eFormidlingService = null, IOptions<AppSettings>? appSettings = null) : IServiceTask
{
    /// <summary>
    /// Executes the service task.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Execute(string taskId, Instance instance)
    {
        ApplicationMetadata applicationMetadata = await appMetadata.GetApplicationMetadata();
        if (appSettings?.Value?.EnableEFormidling == true && applicationMetadata.EFormidling?.SendAfterTaskId == taskId && eFormidlingService != null)
        {
            if (eFormidlingService != null)
            {
                var updatedInstance = await instanceClient.GetInstance(instance);
                await eFormidlingService.SendEFormidlingShipment(updatedInstance);
            }
            else
            {
                logger.LogError("EformidlingService is not configured. No eFormidling shipment will be sent.");
            }
        }
    }
}