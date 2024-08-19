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
public class EFormidlingServiceTaskV2 : ServiceTaskBase
{
    private readonly ILogger<EFormidlingServiceTaskV2> _logger;
    private readonly IInstanceClient _instanceClient;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly IOptions<AppSettings>? _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="EformidlingServiceTask"/> class.
    /// </summary>
    public EFormidlingServiceTaskV2(
        ILogger<EFormidlingServiceTaskV2> logger,
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
    public override string Type => "eFormidling";

    /// <inheritdoc/>
    protected override async Task Execute(string taskId, Instance instance)
    {
        if (_eFormidlingService is null)
        {
            throw new Exception($"No implementation of {nameof(IEFormidlingService)} is added to the DI container.");
        }

        if (_appSettings?.Value.EnableEFormidling is true)
        {
            throw new Exception(
                "When using eFormidling as a service task in the bpmn process, EnableEFormidling should not be set to 'true' in appsettings.json."
            );
        }

        Instance updatedInstance = await _instanceClient.GetInstance(instance);

        _logger.LogDebug("Calling eFormidlingService for eFormidling Service Task {TaskId}.", taskId);
        await _eFormidlingService.SendEFormidlingShipment(updatedInstance);
        _logger.LogDebug("Successfully called eFormidlingService for eFormidling Service Task {TaskId}.", taskId);
    }
}
