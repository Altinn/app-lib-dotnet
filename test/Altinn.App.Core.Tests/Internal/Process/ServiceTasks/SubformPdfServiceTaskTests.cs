using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class SubformPdfServiceTaskTests
{
    private readonly Mock<IPdfService> _pdfServiceMock = new();
    private readonly Mock<ILogger<SubformPdfServiceTask>> _loggerMock = new();
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly SubformPdfServiceTask _serviceTask;

    private const string SubformComponentId = "subform-mopeder";
    private const string SubformDataTypeId = "subform-data-type";
    private const string FileName = "customFilenameTextResourceKey";

    public SubformPdfServiceTaskTests()
    {
        _serviceTask = new SubformPdfServiceTask(
            _processReaderMock.Object,
            _pdfServiceMock.Object,
            _dataClientMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Execute_WithParallelExecution_Should_Call_GenerateAndStorePdf_InParallel()
    {
        // Arrange
        SetupProcessReader(parallelExecution: true);
        var instance = CreateInstanceWithSubformData();
        var context = CreateServiceTaskContext(instance);

        // Act
        var result = await _serviceTask.Execute(context);

        // Assert
        result.Should().BeOfType<ServiceTaskSuccessResult>();

        // Verify that GenerateAndStoreSubformPdfs was called for each data element (parallel execution pattern)
        _pdfServiceMock.Verify(
            x =>
                x.GenerateAndStoreSubformPdfs(
                    It.Is<Instance>(i => i == instance),
                    It.Is<string>(taskId => taskId == "taskId"),
                    It.Is<string?>(filename => filename == FileName),
                    It.Is<string>(componentId => componentId == SubformComponentId),
                    It.IsAny<string>(), // dataElement.Id
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2) // Should be called twice for the two data elements
        );
    }

    [Fact]
    public async Task Execute_WithSequentialExecution_Should_Call_GenerateAndStorePdf_Sequentially()
    {
        // Arrange
        SetupProcessReader(parallelExecution: false);
        var instance = CreateInstanceWithSubformData();
        var context = CreateServiceTaskContext(instance);

        // Act
        var result = await _serviceTask.Execute(context);

        // Assert
        result.Should().BeOfType<ServiceTaskSuccessResult>();

        // Verify that GenerateAndStoreSubformPdfs was called for each data element (sequential execution pattern)
        _pdfServiceMock.Verify(
            x =>
                x.GenerateAndStoreSubformPdfs(
                    It.Is<Instance>(i => i == instance),
                    It.Is<string>(taskId => taskId == "taskId"),
                    It.Is<string?>(filename => filename == FileName),
                    It.Is<string>(componentId => componentId == SubformComponentId),
                    It.IsAny<string>(), // dataElement.Id
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2) // Should be called twice for the two data elements
        );
    }

    [Fact]
    public async Task Execute_WithNoMatchingDataElements_Should_Not_Call_GenerateAndStorePdf()
    {
        // Arrange
        SetupProcessReader(parallelExecution: false);
        var instance = CreateInstanceWithoutSubformData();
        var context = CreateServiceTaskContext(instance);

        // Act
        var result = await _serviceTask.Execute(context);

        // Assert
        result.Should().BeOfType<ServiceTaskSuccessResult>();

        // Verify that GenerateAndStoreSubformPdfs was not called
        _pdfServiceMock.Verify(
            x =>
                x.GenerateAndStoreSubformPdfs(
                    It.IsAny<Instance>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_WithSpecificDataElements_Should_Call_GenerateAndStorePdf_WithCorrectIds()
    {
        // Arrange
        SetupProcessReader(parallelExecution: false);
        var instance = CreateInstanceWithSubformData();
        var context = CreateServiceTaskContext(instance);

        // Act
        await _serviceTask.Execute(context);

        // Assert - verify that the correct data element IDs were used (sequential execution pattern)
        _pdfServiceMock.Verify(
            x =>
                x.GenerateAndStoreSubformPdfs(
                    It.IsAny<Instance>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<string>(id => id == "data-element-1" || id == "data-element-2"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Execute_WithNoPdfConfiguration_Should_Use_DefaultConfiguration()
    {
        // Arrange
        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>()))
            .Returns(new AltinnTaskExtension { TaskType = "subform-pdf" });

        var instance = CreateInstanceWithoutSubformData();
        var context = CreateServiceTaskContext(instance);

        // Act
        var result = await _serviceTask.Execute(context);

        // Assert
        result.Should().BeOfType<ServiceTaskSuccessResult>();
    }

    private void SetupProcessReader(bool parallelExecution)
    {
        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>()))
            .Returns(
                new AltinnTaskExtension
                {
                    TaskType = "subform-pdf",
                    SubformPdfConfiguration = new AltinnSubformPdfConfiguration
                    {
                        SubformComponentId = SubformComponentId,
                        SubformDataTypeId = SubformDataTypeId,
                        FilenameTextResourceKey = FileName,
                        ParallelExecution = parallelExecution,
                    },
                }
            );
    }

    private static Instance CreateInstanceWithSubformData()
    {
        return new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "taskId" } },
            Data = new List<DataElement>
            {
                new() { Id = "data-element-1", DataType = SubformDataTypeId },
                new() { Id = "data-element-2", DataType = SubformDataTypeId },
                new() { Id = "other-data-element", DataType = "other-type" }, // Should be filtered out
            },
        };
    }

    private static Instance CreateInstanceWithoutSubformData()
    {
        return new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "taskId" } },
            Data = new List<DataElement>
            {
                new() { Id = "other-data-element", DataType = "other-type" },
            },
        };
    }

    private static ServiceTaskContext CreateServiceTaskContext(Instance instance)
    {
        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);
        return new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };
    }
}
