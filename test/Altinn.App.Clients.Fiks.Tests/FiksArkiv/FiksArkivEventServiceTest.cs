using System.Linq.Expressions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivEventServiceTest
{
    [Fact]
    public async Task ExecuteAsync_StopsWhenCancellationRequested()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FiksArkivEventService>>();

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
        });

        var service = fixture.FiksArkivEventService;
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await cts.CancelAsync();

        // Assert
        fixture.ExternalFiksIOClientMock.Verify(x => x.DisposeAsync(), Times.Once);
        loggerMock.Verify(
            MatchLogEntry(LogLevel.Information, "Fiks Arkiv Service stopping.", loggerMock.Object),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteAsync_PerformsHealthCheck_ReconnectsWhenRequired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FiksArkivEventService>>();
        var fakeTime = new FakeTimeProvider();

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
            services.AddSingleton<TimeProvider>(fakeTime);
        });

        var service = fixture.FiksArkivEventService;
        using var cts = new CancellationTokenSource();
        fixture.ExternalFiksIOClientMock.Setup(x => x.IsOpenAsync()).ReturnsAsync(false);

        // Act
        await service.StartAsync(cts.Token);
        fakeTime.Advance(TimeSpan.FromMinutes(10));
        await cts.CancelAsync();

        // Assert
        fixture.ExternalFiksIOClientMock.Verify(x => x.IsOpenAsync(), Times.Once);
        fixture.ExternalFiksIOClientMock.Verify(x => x.DisposeAsync(), Times.Exactly(2));
        loggerMock.Verify(
            MatchLogEntry(LogLevel.Error, "FiksIO Client is unhealthy, reconnecting.", loggerMock.Object),
            Times.Once
        );
        loggerMock.Verify(
            MatchLogEntry(LogLevel.Information, "Fiks Arkiv Service stopping.", loggerMock.Object),
            Times.Once
        );
    }

    // TODO: Add test for MessageReceivedHandler (which uses RetrieveInstance and ParseCorrelationId)

    private static Expression<Action<ILogger<T>>> MatchLogEntry<T>(
        LogLevel logLevel,
        string partialMessage,
        ILogger<T> _
    )
        where T : class
    {
        return x =>
            x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(
                    (v, _) => v.ToString()!.Contains(partialMessage, StringComparison.OrdinalIgnoreCase)
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            );
    }
}
