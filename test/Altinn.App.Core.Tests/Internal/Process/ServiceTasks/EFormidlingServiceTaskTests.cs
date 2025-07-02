using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class EFormidlingServiceTaskTests
{
    private readonly Mock<ILogger<EFormidlingServiceTask>> _loggerMock = new();
    private readonly Mock<IEFormidlingService> _eFormidlingServiceMock = new();
    private readonly Mock<IOptions<AppSettings>> _appSettingsMock = new();
    private readonly EFormidlingServiceTask _serviceTask;

    public EFormidlingServiceTaskTests()
    {
        _serviceTask = new EFormidlingServiceTask(
            _loggerMock.Object,
            _eFormidlingServiceMock.Object,
            _appSettingsMock.Object
        );
    }

    [Fact]
    public async Task Execute_Should_LogWarning_When_EFormidlingDisabled()
    {
        // Arrange
        Instance instance = GetInstance();
        var appSettings = new AppSettings { EnableEFormidling = false };
        _appSettingsMock.Setup(x => x.Value).Returns(appSettings);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskParameters { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains(
                                    "EFormidling has been added as a service task in the BPMN process definition but is not enabled in appsettings.json. No eFormidling shipment will be sent, but the service task will be completed."
                                )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_ThrowException_When_EFormidlingServiceIsNull()
    {
        // Arrange
        Instance instance = GetInstance();

        var appSettings = new AppSettings { EnableEFormidling = true };
        _appSettingsMock.Setup(x => x.Value).Returns(appSettings);

        var serviceTask = new EFormidlingServiceTask(_loggerMock.Object, null, _appSettingsMock.Object);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskParameters { InstanceDataMutator = instanceMutatorMock.Object };

        // Act & Assert
        await Assert.ThrowsAsync<ProcessException>(() => serviceTask.Execute(parameters));
    }

    [Fact]
    public async Task Execute_Should_Call_SendEFormidlingShipment_When_EFormidlingEnabled()
    {
        // Arrange
        Instance instance = GetInstance();

        var appSettings = new AppSettings { EnableEFormidling = true };
        _appSettingsMock.Setup(x => x.Value).Returns(appSettings);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskParameters { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(x => x.SendEFormidlingShipment(instance), Times.Once);
    }

    private static Instance GetInstance()
    {
        return new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "taskId" } },
        };
    }
}
