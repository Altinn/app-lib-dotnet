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
[Route("/process-engine/{org}/{app}/{instanceOwnerPartyId:int}/{instanceGuid:guid}")]
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
        string org,
        string app,
        int instanceOwnerPartyId,
        Guid instanceGuid,
        [FromBody] ProcessNextRequest request
    )
    {
        var instanceInformation = new InstanceInformation
        {
            Org = org,
            App = app,
            InstanceOwnerPartyId = instanceOwnerPartyId,
            InstanceGuid = instanceGuid,
        };

        var processEngineRequest = request.ToProcessEngineRequest(instanceInformation);

        if (_processEngine.HasDuplicateJob(processEngineRequest.JobIdentifier))
            return Ok(); // TODO: 200-OK for duplicates. Perhaps this should be another code at some points?

        var response = await _processEngine.EnqueueJob(processEngineRequest);
        return response.IsAccepted() ? Ok() : BadRequest(response.Message);
    }

    /// <summary>
    /// Get the status of jobs for a specific instance.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ProcessEngineStatusResponse> Status(
        string org,
        string app,
        int instanceOwnerPartyId,
        Guid instanceGuid
    )
    {
        var instanceInformation = new InstanceInformation
        {
            Org = org,
            App = app,
            InstanceOwnerPartyId = instanceOwnerPartyId,
            InstanceGuid = instanceGuid,
        };

        var job = _processEngine.GetJobForInstance(instanceInformation);
        if (job is null)
            return NoContent(); // 204 - No job for this instance

        var response = new ProcessEngineStatusResponse
        {
            InstanceInformation = instanceInformation,
            OverallStatus = job.OverallStatus(),
            Tasks = job.Tasks.Select(ProcessEngineTaskDetail.FromProcessEngineTask).ToList(),
        };

        return Ok(response);
    }
}
