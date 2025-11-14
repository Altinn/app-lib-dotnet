using System.Collections.Concurrent;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.ProcessEngine.Controllers;

/// <summary>
/// Controller for handling incoming process engine requests.
/// </summary>
[ApiController]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine/test")]
public class TestController : ControllerBase
{
    private readonly ILogger<ProcessEngineController> _logger;
    private readonly IProcessEngine _processEngine;
    private readonly IServiceProvider _serviceProvider;
    private static int _scenarioCallbackCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineController"/> class.
    /// </summary>
    public TestController(IServiceProvider serviceProvider, ILogger<ProcessEngineController> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _processEngine = serviceProvider.GetRequiredService<IProcessEngine>();
    }

    private readonly IReadOnlyList<TestScenario> _testScenarios =
    [
        new(new ProcessEngineCommand.Noop(), ProcessEngineRetryStrategy.None()),
        new(new ProcessEngineCommand.Throw(), ProcessEngineRetryStrategy.None()),
        new(
            [
                new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(0.5)),
                new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(0.5)),
            ],
            ProcessEngineRetryStrategy.None()
        ),
        new(
            [
                new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(1)),
                new ProcessEngineCommand.Webhook("/process-engine/test/scenario-callback"),
            ],
            ProcessEngineRetryStrategy.None()
        ),
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
        InstanceInformation instanceInfo = new(org, app, instanceOwnerPartyId, instanceGuid);

        var requests = Enumerable
            .Range(1, numJobs)
            .Select(i => new ProcessEngineRequest(
                $"job-identifier-{i}",
                instanceInfo,
                new ProcessEngineActor("callers-altinn-party-id?", "nb"),
                _testScenarios[testScenario].ToCommandRequests(instanceInfo)
            ));

        _scenarioCallbackCounter = 0;
        var tasks = requests.Select(async x =>
        {
            var response = await _processEngine.EnqueueJob(x);
            responses.Add(response);
        });

        await Task.WhenAll(tasks);

        // Wait for queue to finish?
        while (block && _processEngine.InboxCount > 0)
            await Task.Delay(50);

        return responses.All(x => x.IsAccepted())
            ? Ok(new { NumResponses = responses.Count })
            : BadRequest(responses.First().Message);
    }

    [HttpGet("scenario-callback")]
    public ActionResult ScenarioCallback()
    {
        Interlocked.Increment(ref _scenarioCallbackCounter);

        return Ok();
    }

    [HttpGet("scenario-stats")]
    public ActionResult ScenarioStats()
    {
        return Ok(new { Counter = _scenarioCallbackCounter });
    }

    private sealed record TestScenario
    {
        public IEnumerable<ProcessEngineCommand> Commands { get; init; }
        public ProcessEngineRetryStrategy RetryStrategy { get; init; }

        public TestScenario(ProcessEngineCommand command, ProcessEngineRetryStrategy retryStrategy)
        {
            Commands = [command];
            RetryStrategy = retryStrategy;
        }

        public TestScenario(IEnumerable<ProcessEngineCommand> commands, ProcessEngineRetryStrategy retryStrategy)
        {
            Commands = commands;
            RetryStrategy = retryStrategy;
        }

        public IEnumerable<ProcessEngineCommandRequest> ToCommandRequests(InstanceInformation instanceInformation)
        {
            var uriPrefix =
                $"http://local.altinn.cloud/{instanceInformation.Org}/{instanceInformation.App}/instances/{instanceInformation.InstanceOwnerPartyId}/{instanceInformation.InstanceGuid}";

            foreach (var command in Commands)
            {
                var cmd = command is ProcessEngineCommand.Webhook webhook
                    ? webhook with
                    {
                        Uri = $"{uriPrefix}/{webhook.Uri}",
                    }
                    : command;

                yield return new ProcessEngineCommandRequest(cmd, RetryStrategy: RetryStrategy);
            }
        }
    }
}
