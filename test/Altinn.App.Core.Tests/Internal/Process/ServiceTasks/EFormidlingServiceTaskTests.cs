using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Implementation;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class EFormidlingServiceTaskTests
{
    private readonly Mock<ILogger<EFormidlingServiceTask>> _loggerMock = new();
    private readonly Mock<IEFormidlingService> _eFormidlingServiceMock = new();
    private readonly Mock<IOptions<AppSettings>> _appSettingsMock = new();
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly Mock<IHostEnvironment> _hostEnvironmentMock = new();
    private readonly Mock<IEFormidlingConfigurationProvider> _eFormidlingConfigurationProvider = new();
    private readonly EFormidlingServiceTask _serviceTask;

    public EFormidlingServiceTaskTests()
    {
        _hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        _serviceTask = new EFormidlingServiceTask(
            _loggerMock.Object,
            _processReaderMock.Object,
            _hostEnvironmentMock.Object,
            _eFormidlingServiceMock.Object,
            _eFormidlingConfigurationProvider.Object
        );
    }

    [Fact]
    public async Task Execute_Should_BeEnabled_When_NoBpmnConfig()
    {
        // Arrange
        Instance instance = GetInstance();

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(
            x => x.SendEFormidlingShipment(instance, It.IsAny<ValidAltinnEFormidlingConfiguration>()),
            Times.Once
        );
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains(
                                    "No eFormidling configuration found in BPMN for task taskId. Defaulting to enabled"
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

        var serviceTask = new EFormidlingServiceTask(
            _loggerMock.Object,
            _processReaderMock.Object,
            _hostEnvironmentMock.Object,
            null,
            _eFormidlingConfigurationProvider.Object
        );

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

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

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(
            x => x.SendEFormidlingShipment(instance, It.IsAny<ValidAltinnEFormidlingConfiguration>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_UseEnvironmentSpecificBpmnConfig_When_Configured()
    {
        // Arrange
        Instance instance = GetInstance();

        var eFormidlingConfig = new AltinnEFormidlingConfiguration
        {
            Enabled =
            [
                new AltinnEnvironmentConfig { Environment = "prod", Value = "true" },
                new AltinnEnvironmentConfig { Environment = "staging", Value = "false" },
            ],
        };

        var taskExtension = new AltinnTaskExtension { EFormidlingConfiguration = eFormidlingConfig };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("taskId")).Returns(taskExtension);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(
            x => x.SendEFormidlingShipment(instance, It.IsAny<ValidAltinnEFormidlingConfiguration>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_SkipExecution_When_BpmnConfigDisabled()
    {
        // Arrange
        Instance instance = GetInstance();

        var eFormidlingConfig = new AltinnEFormidlingConfiguration
        {
            Enabled = [new AltinnEnvironmentConfig { Environment = "prod", Value = "false" }],
        };

        var taskExtension = new AltinnTaskExtension { EFormidlingConfiguration = eFormidlingConfig };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("taskId")).Returns(taskExtension);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(x => x.SendEFormidlingShipment(It.IsAny<Instance>()), Times.Never);
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EFormidling is disabled for task taskId")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_BeEnabled_When_NoBpmnConfigExplicit()
    {
        // Arrange
        Instance instance = GetInstance();

        // No BPMN configuration (explicit null)
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("taskId")).Returns((AltinnTaskExtension?)null);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(
            x => x.SendEFormidlingShipment(instance, It.IsAny<ValidAltinnEFormidlingConfiguration>()),
            Times.Once
        );
        _loggerMock.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains(
                                    "No eFormidling configuration found in BPMN for task taskId. Defaulting to enabled"
                                )
                    ),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_Should_UseGlobalBpmnConfig_When_NoEnvironmentSpecific()
    {
        // Arrange
        Instance instance = GetInstance();

        var eFormidlingConfig = new AltinnEFormidlingConfiguration
        {
            Enabled =
            [
                new AltinnEnvironmentConfig { Value = "true" }, // Global config (no env specified)
            ],
        };

        var taskExtension = new AltinnTaskExtension { EFormidlingConfiguration = eFormidlingConfig };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("taskId")).Returns(taskExtension);

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _eFormidlingServiceMock.Verify(
            x => x.SendEFormidlingShipment(instance, It.IsAny<ValidAltinnEFormidlingConfiguration>()),
            Times.Once
        );
    }

    private static Instance GetInstance()
    {
        return new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "taskId" } },
        };
    }
}
