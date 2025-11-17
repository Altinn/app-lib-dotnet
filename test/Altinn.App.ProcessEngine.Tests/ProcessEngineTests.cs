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
                    new ProcessEngineRequest(
                        $"test-job-{i}",
                        new InstanceInformation("test-org", "test-app", 1234, Guid.NewGuid()),
                        new ProcessEngineActor("501337"),
                        [
                            new ProcessEngineCommandRequest(
                                new ProcessEngineCommand.Delegate(
                                    (job, task, ct) =>
                                    {
                                        testResults.Enqueue($"{job.Identifier}-task-delegate-1");
                                        return Task.CompletedTask;
                                    }
                                )
                            ),
                            new ProcessEngineCommandRequest(
                                new ProcessEngineCommand.Delegate(
                                    (job, task, ct) =>
                                    {
                                        testResults.Enqueue($"{job.Identifier}-task-delegate-2");
                                        return Task.CompletedTask;
                                    }
                                )
                            ),
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
