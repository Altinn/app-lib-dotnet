using System.Collections.Concurrent;
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
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine/test")]
public class TestController : ControllerBase
{
    private readonly ILogger<ProcessEngineController> _logger;
    private readonly IProcessEngine _processEngine;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineController"/> class.
    /// </summary>
    public TestController(IServiceProvider serviceProvider, ILogger<ProcessEngineController> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processEngine = serviceProvider.GetRequiredService<IProcessEngine>();
    }

    private sealed record TestScenario(ProcessEngineCommand Command, ProcessEngineRetryStrategy RetryStrategy);

    private readonly IReadOnlyList<TestScenario> _testScenarios =
    [
        new(new ProcessEngineCommand.Noop(), ProcessEngineRetryStrategy.None()),
        new(new ProcessEngineCommand.Delay(TimeSpan.FromSeconds(1)), ProcessEngineRetryStrategy.None()),
        new(new ProcessEngineCommand.Throw(), ProcessEngineRetryStrategy.None()),
    ];

    [HttpPost("scenario")]
    public async Task<ActionResult> Scenario(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] int numJobs = 1000,
        [FromQuery] int testScenario = 0,
        [FromQuery] bool block = true
    )
    {
        ConcurrentBag<ProcessEngineResponse> responses = [];
        var requests = Enumerable
            .Range(1, numJobs)
            .Select(i => new ProcessEngineRequest(
                $"job-identifier-{i}",
                new InstanceInformation(org, app, instanceOwnerPartyId, instanceGuid),
                new ProcessEngineActor("nb", "callers-altinn-party-id?"),
                [
                    new ProcessEngineCommandRequest(
                        _testScenarios[testScenario].Command,
                        RetryStrategy: _testScenarios[testScenario].RetryStrategy
                    ),
                ]
            ));

        var tasks = requests.Select(async x =>
        {
            var response = await _processEngine.EnqueueJob(x);
            responses.Add(response);
        });

        await Task.WhenAll(tasks);

        // await Parallel.ForEachAsync(
        //     requests,
        //     async (request, ct) =>
        //     {
        //         var response = await _processEngine.EnqueueJob(request, ct);
        //         responses.Add(response);
        //     }
        // );

        // Wait for queue to finish?
        while (block && _processEngine.InboxCount > 0)
        {
            await Task.Delay(50);
        }

        return responses.All(x => x.IsAccepted()) ? Ok() : BadRequest(responses.First().Message);
    }
}
