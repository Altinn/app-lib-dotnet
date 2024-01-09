using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

public class EformidlingServiceTask: IServiceTask
{
    private readonly ILogger<EformidlingServiceTask> _logger;
    private readonly IAppMetadata _appMetadata;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly IInstanceClient _instanceClient;
    private readonly AppSettings? _appSettings;

    public EformidlingServiceTask(ILogger<EformidlingServiceTask> logger, IAppMetadata appMetadata, IInstanceClient instanceClient, IEFormidlingService? eFormidlingService = null, IOptions<AppSettings>? appSettings = null)
    {
        _logger = logger;
        _appMetadata = appMetadata;
        _eFormidlingService = eFormidlingService;
        _instanceClient = instanceClient;
        _appSettings = appSettings?.Value;
    }


    public async Task Execute(string taskId, Instance instance)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        if (_appSettings?.EnableEFormidling == true && appMetadata.EFormidling?.SendAfterTaskId == taskId && _eFormidlingService != null)
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