using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.ProcessEngine.Controllers;

/// <summary>
/// Controller for handling incoming process engine requests.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKeySchemeName)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine")]
public class ProcessEngineController : ControllerBase
{
    private readonly ILogger<ProcessEngineController> _logger;
    private readonly IProcessEngine _processEngine;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineController"/> class.
    /// </summary>
    public ProcessEngineController(IServiceProvider serviceProvider, ILogger<ProcessEngineController> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processEngine = serviceProvider.GetRequiredService<IProcessEngine>();
    }

    /// <summary>
    /// Enqueue a request to move the process forward.
    /// </summary>
    [HttpPost("next")]
    public async Task<ActionResult> Next(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromBody] ProcessNextRequest request
    )
    {
        var processEngineRequest = request.ToProcessEngineRequest(
            new InstanceInformation(org, app, instanceOwnerPartyId, instanceGuid)
        );

        if (_processEngine.HasQueuedJob(processEngineRequest.JobIdentifier))
            return Ok(); // 200-OK for duplicates. Perhaps this should be another code at some points?

        var response = await _processEngine.EnqueueJob(processEngineRequest);
        return response.IsAccepted() ? Ok() : BadRequest(response.Message);
    }
}
