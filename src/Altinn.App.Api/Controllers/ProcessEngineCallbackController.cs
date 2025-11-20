using Altinn.App.Core.Models;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling process engine callbacks.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKeySchemeName)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine-callback")]
public class ProcessEngineCallbackController : ControllerBase
{
    private readonly ILogger<ProcessEngineCallbackController> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineCallbackController"/> class.
    /// </summary>
    public ProcessEngineCallbackController(
        IServiceProvider serviceProvider,
        ILogger<ProcessEngineCallbackController> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes a command based on the provided command key.
    /// </summary>
    [HttpPost("{commandKey}")]
    public async Task<IActionResult> ExecuteCommand(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string commandKey,
        [FromBody] ProcessEngineAppCallbackPayload payload
    )
    {
        var appId = new AppIdentifier(org, app);
        var instanceId = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        var handler =
            _serviceProvider.GetServices<IProcessEngineCallbackHandler>().FirstOrDefault(x => x.Key == commandKey)
            ?? throw new KeyNotFoundException("yikes");

        var result = await handler.Execute(appId, instanceId, payload);

        return result ? Ok() : BadRequest();
    }
}

// TODO: Move this somewhere more reasonable and create some implementations
internal interface IProcessEngineCallbackHandler
{
    string Key { get; }
    Task<bool> Execute(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        ProcessEngineAppCallbackPayload payload
    );
};
