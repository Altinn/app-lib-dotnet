using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class PdfServiceTaskTests
{
    private readonly Mock<IPdfService> _pdfServiceMock = new();
    private readonly Mock<ILogger<PdfServiceTask>> _loggerMock = new();
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly PdfServiceTask _serviceTask;

    private const string FileName = "My file name";

    public PdfServiceTaskTests()
    {
        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>()))
            .Returns(
                new AltinnTaskExtension
                {
                    TaskType = "pdf",
                    PdfConfiguration = new AltinnPdfConfiguration { Filename = FileName },
                }
            );

        _serviceTask = new PdfServiceTask(_pdfServiceMock.Object, _processReaderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_Should_Call_GenerateAndStorePdf()
    {
        // Arrange
        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "taskId" } },
        };

        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        instanceMutatorMock.Setup(x => x.Instance).Returns(instance);

        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };

        // Act
        await _serviceTask.Execute(parameters);

        // Assert
        _pdfServiceMock.Verify(
            x =>
                x.GenerateAndStorePdf(
                    instance,
                    instance.Process.CurrentTask.ElementId,
                    FileName,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
