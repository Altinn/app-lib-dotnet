using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
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

    public PdfServiceTaskTests()
    {
        _serviceTask = new PdfServiceTask(_loggerMock.Object, _pdfServiceMock.Object, _processReaderMock.Object);
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
