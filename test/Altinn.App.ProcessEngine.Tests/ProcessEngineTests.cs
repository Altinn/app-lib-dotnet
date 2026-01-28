using System.Collections.Concurrent;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests;

public class ProcessEngineTests
{
    [Fact]
    public async Task ProcessEngine_ExecutesJobsAndCommands_AsExpected()
    {
        // Arrange
        ConcurrentQueue<string> testResults = new();
        await using var fixture = TestFixture.Create(builder =>
            builder.Services.ConfigureProcessEngine(options =>
            {
                options.QueueCapacity = 50;
            })
        );

        // Act
        await fixture.ProcessEngine.Start();
        var tasks = Enumerable
            .Range(0, 100)
            .Select(async i =>
                await fixture.ProcessEngine.EnqueueJob(
                    new ProcessEngineJobRequest(
                        $"test-job-{i}",
                        new InstanceInformation
                        {
                            Org = "test-org",
                            App = "test-app",
                            InstanceOwnerPartyId = 1234,
                            InstanceGuid = Guid.NewGuid(),
                        },
                        new ProcessEngineActor { UserIdOrOrgNumber = "501337" },
                        [
                            new ProcessEngineCommandRequest
                            {
                                Command = new ProcessEngineCommand.Delegate(
                                    (job, task, ct) =>
                                    {
                                        testResults.Enqueue($"{job.Key}-task-delegate-1");
                                        return Task.CompletedTask;
                                    }
                                ),
                            },
                            new ProcessEngineCommandRequest
                            {
                                Command = new ProcessEngineCommand.Delegate(
                                    (job, task, ct) =>
                                    {
                                        testResults.Enqueue($"{job.Key}-task-delegate-2");
                                        return Task.CompletedTask;
                                    }
                                ),
                            },
                        ]
                    )
                )
            )
            .ToList();

        await Task.WhenAll(tasks);

        while (fixture.ProcessEngine.InboxCount > 0)
            await Task.Delay(50);

        // Assert
        Assert.All(tasks, task => Assert.True(task.Result.IsAccepted()));
        Assert.Equal(200, testResults.Count);
    }
}
