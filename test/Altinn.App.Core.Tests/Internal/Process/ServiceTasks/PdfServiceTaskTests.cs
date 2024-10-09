using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class PdfServiceTaskTests
{
    private readonly Mock<IPdfService> _pdfServiceMock;
    private readonly PdfServiceTask _serviceTask;

    public PdfServiceTaskTests()
    {
        Mock<ILogger<PdfServiceTask>> loggerMock = new();
        Mock<IProcessReader> processReaderMock = new();
        _pdfServiceMock = new Mock<IPdfService>();

        _serviceTask = new PdfServiceTask(loggerMock.Object, _pdfServiceMock.Object, processReaderMock.Object);
    }

    [Fact]
    public async Task Execute_Should_Call_GenerateAndStorePdf()
    {
        // Arrange
        var instance = new Instance();
        var taskId = "taskId";

        // Act
        await _serviceTask.Execute(taskId, instance);

        // Assert
        _pdfServiceMock.Verify(x => x.GenerateAndStorePdf(instance, taskId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
