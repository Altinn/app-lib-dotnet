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
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine-callbacks")]
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
    /// Get the data elements being signed in the current signature task.
    /// </summary>
    /// <param name="org">unique identifier of the organization responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organization</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that this the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="commandKey"></param>
    /// <param name="payload"></param>
    /// <returns>An object containing the documents to be signed</returns>
    [HttpPost("{commandKey}")]
    public async Task<IActionResult> ExecuteCommand(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string commandKey,
        [FromBody] ProcessEngineCallbackPayload payload
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
        ProcessEngineCallbackPayload payload
    );
};
