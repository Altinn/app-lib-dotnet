using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Process.V2;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using IProcessEngine = Altinn.App.Core.Internal.Process.V2.IProcessEngine;

namespace Altinn.App.Core.Tests.Internal.Process.V2;

public class ProcessEngineTest
{
    [Fact]
    public async Task StartProcess_returns_unsuccessful_when_process_already_started()
    {
        IProcessEngine processEngine = GetProcessEngine();
        Instance instance = new Instance() { Process = new ProcessState() { CurrentTask = new ProcessElementInfo() { ElementId = "Task_1" } } };
        ProcessStartRequest processStartRequest = new ProcessStartRequest() { Instance = instance };
        ProcessChangeResult result = await processEngine.StartProcess(processStartRequest);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Process is already started. Use next.");
        result.ErrorType.Should().Be("Conflict");
    }
    
    [Fact]
    public async Task StartProcess_returns_unsuccessful_when_no_matching_startevent_found()
    {
        Mock<IProcessReader> processReaderMock = new();
        processReaderMock.Setup(r => r.GetStartEventIds()).Returns(new List<string>() { "StartEvent_1" });
        IProcessEngine processEngine = GetProcessEngine(processReaderMock);
        Instance instance = new Instance();
        ProcessStartRequest processStartRequest = new ProcessStartRequest() { Instance = instance, StartEventId = "NotTheStartEventYouAreLookingFor" };
        ProcessChangeResult result = await processEngine.StartProcess(processStartRequest);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No matching startevent");
        result.ErrorType.Should().Be("Conflict");
    }

    private static IProcessEngine GetProcessEngine(Mock<IProcessReader>? processReaderMock = null)
    {
        processReaderMock ??= new();
        Mock<IInstance> instanceMock = new();
        Mock<IProfile> profileMock = new();
        Mock<IProcess> processMock = new();
        Mock<IAppEvents> appEventsMock = new();
        Mock<ITaskEvents> taskEventsMock = new();
        Mock<IProcessNavigator> processNavigatorMock = new();
        Mock<IEvents> eventsMock = new();
        IOptions<AppSettings> appSettings = Options.Create(new AppSettings() { RegisterEventsWithEventsComponent = true });
        ILogger<ProcessEngine> logger = new NullLogger<ProcessEngine>();
        return new ProcessEngine(
            instanceMock.Object,
            processReaderMock.Object,
            profileMock.Object,
            processMock.Object,
            appEventsMock.Object,
            taskEventsMock.Object,
            processNavigatorMock.Object,
            eventsMock.Object,
            appSettings,
            logger);
    }
}
