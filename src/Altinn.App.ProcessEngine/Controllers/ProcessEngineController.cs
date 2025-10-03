using System.Collections.Concurrent;
using System.Globalization;
// using Altinn.App.Core.Internal.App;
// using Altinn.App.Core.Internal.Instances;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.ProcessEngine.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKeySchemeName)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine")]
public class ProcessEngineController : ControllerBase
{
    private readonly ILogger<ProcessEngineController> _logger;
    private readonly IProcessEngine _processEngine;

    // private readonly IInstanceClient _instanceClient;
    // private readonly IAppMetadata _appMetadata;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The data controller is responsible for adding business logic to the data elements.
    /// </summary>
    public ProcessEngineController(IServiceProvider serviceProvider, ILogger<ProcessEngineController> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processEngine = serviceProvider.GetRequiredService<IProcessEngine>();
        // _instanceClient = serviceProvider.GetRequiredService<IInstanceClient>();
        // _appMetadata = serviceProvider.GetRequiredService<IAppMetadata>();
    }

    [HttpPost("test")]
    public async Task<ActionResult> Post(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] int numJobs = 1
    )
    {
        // TODO: InstanceClient needs the ability to use Maskinporten auth
        // var instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        var instance = new Instance
        {
            Id = $"{instanceOwnerPartyId}/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerPartyId.ToString(CultureInfo.InvariantCulture) },
        };

        ConcurrentBag<ProcessEngineResponse> responses = [];
        var requests = Enumerable
            .Range(1, numJobs)
            .Select(i => new ProcessEngineRequest(
                $"job-identifier-{i}",
                instance,
                [
                    new ProcessEngineTaskRequest(
                        $"task-identifier-{i}",
                        new ProcessEngineTaskCommand.MoveProcessForward("Task_1", "Task_2"),
                        RetryStrategy: ProcessEngineRetryStrategy.Constant(TimeSpan.FromSeconds(1), 10)
                    ),
                    new ProcessEngineTaskRequest(
                        "doesnt-matter-since-the-other-will-fail",
                        new ProcessEngineTaskCommand.ExecuteServiceTask(string.Empty)
                    ),
                ]
            ));

        await Parallel.ForEachAsync(
            requests,
            async (request, ct) =>
            {
                var response = await _processEngine.EnqueueJob(request, ct);
                responses.Add(response);
            }
        );

        return responses.All(x => x.IsAccepted()) ? Ok() : BadRequest(responses.First().Message);
    }
}
