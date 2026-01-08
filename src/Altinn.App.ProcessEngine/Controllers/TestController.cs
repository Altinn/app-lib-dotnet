using System.Collections.Concurrent;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.ProcessEngine.Controllers;

/// <summary>
/// Controller for handling incoming process engine requests.
/// </summary>
[ApiController]
[Route("/process-engine/test")]
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
        new(new ProcessEngineCommand.Noop()),
        new(new ProcessEngineCommand.Throw()),
        new([
            new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(0.5)),
            new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(0.5)),
        ]),
        new([
            new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(1)),
            new ProcessEngineCommand.Webhook("/process-engine/test/scenario-callback"),
        ]),
        new([
            new ProcessEngineCommand.Timeout(TimeSpan.FromSeconds(1)),
            new ProcessEngineCommand.Delegate(
                (job, task, ct) =>
                {
                    Interlocked.Increment(ref _scenarioCallbackCounter);
                    return Task.CompletedTask;
                }
            ),
        ]),
    ];

    [HttpPost("scenario")]
    public async Task<ActionResult> Scenario(
        [FromQuery] int numJobs = 1000,
        [FromQuery] int testScenario = 0,
        [FromQuery] bool block = true
    )
    {
        ConcurrentBag<ProcessEngineResponse> responses = [];
        InstanceInformation instanceInfo = new()
        {
            Org = "test-org",
            App = "test-app",
            InstanceOwnerPartyId = 12345,
            InstanceGuid = Guid.NewGuid(),
        };

        var requests = Enumerable
            .Range(1, numJobs)
            .Select(i => new ProcessEngineJobRequest(
                $"job-identifier-{i}",
                instanceInfo,
                new ProcessEngineActor { UserIdOrOrgNumber = "callers-altinn-party-id?" },
                _testScenarios[testScenario].ToCommandRequests()
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

        public TestScenario(ProcessEngineCommand command, ProcessEngineRetryStrategy? retryStrategy = null)
        {
            Commands = [command];
            RetryStrategy = retryStrategy ?? ProcessEngineRetryStrategy.None();
        }

        public TestScenario(
            IEnumerable<ProcessEngineCommand> commands,
            ProcessEngineRetryStrategy? retryStrategy = null
        )
        {
            Commands = commands;
            RetryStrategy = retryStrategy ?? ProcessEngineRetryStrategy.None();
        }

        public IEnumerable<ProcessEngineCommandRequest> ToCommandRequests() =>
            Commands.Select(command => new ProcessEngineCommandRequest
            {
                Command = command,
                RetryStrategy = RetryStrategy,
            });
    }
}
