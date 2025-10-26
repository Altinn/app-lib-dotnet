using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivDefaultAutoSendDecisionTest
{
    [Fact]
    public async Task ShouldSend_UsesConfig_MakesCorrectDecision()
    {
        // Arrage
        var settings = new FiksArkivSettings { AutoSend = new FiksArkivAutoSendSettings { AfterTaskId = "Task_1" } };
        var decisionHandler = new FiksArkivDefaultAutoSendDecision(Options.Create(settings));

        // Act
        var expectSend = await decisionHandler.ShouldSend("Task_1", null!);
        var expectNotSend = await decisionHandler.ShouldSend("Task_2", null!);

        // Assert
        Assert.True(expectSend);
        Assert.False(expectNotSend);
    }

    [Fact]
    public async Task ShouldSend_HandlesMissingConfig()
    {
        // Arrage
        var settings = new FiksArkivSettings();
        var decisionHandler = new FiksArkivDefaultAutoSendDecision(Options.Create(settings));

        // Act
        var expectNotSend = await decisionHandler.ShouldSend("Task_1", null!);

        // Assert
        Assert.Null(settings.AutoSend);
        Assert.False(expectNotSend);
    }
}
