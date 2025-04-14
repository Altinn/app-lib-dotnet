using System.Linq.Expressions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using KS.Fiks.IO.Client.Configuration;
using Ks.Fiks.Maskinporten.Client;
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
        var (clientFactoryMock, createdClients) = GetFixIOClientFactoryMock();

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
            services.AddTransient<IFiksIOClientFactory>(sp => clientFactoryMock.Object);
        });

        var service = fixture.FiksArkivEventService;
        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await cts.CancelAsync();

        // Assert
        Assert.Single(createdClients);
        createdClients[0].Verify(x => x.DisposeAsync(), Times.Once);
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
        var (clientFactoryMock, createdClients) = GetFixIOClientFactoryMock(x =>
            x.Setup(m => m.IsOpenAsync()).ReturnsAsync(false)
        );

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
            services.AddTransient<IFiksIOClientFactory>(sp => clientFactoryMock.Object);
            services.AddSingleton<TimeProvider>(fakeTime);
        });

        var service = fixture.FiksArkivEventService;

        // Act
        await service.StartAsync(CancellationToken.None);
        fakeTime.Advance(TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(2, createdClients.Count);
        clientFactoryMock.Verify(
            x =>
                x.CreateClient(
                    It.IsAny<FiksIOConfiguration>(),
                    It.IsAny<IMaskinportenClient>(),
                    It.IsAny<ILoggerFactory>()
                ),
            Times.Exactly(2)
        );
        createdClients[0].Verify(x => x.IsOpenAsync(), Times.Once);
        createdClients[0].Verify(x => x.DisposeAsync(), Times.Once);
        loggerMock.Verify(
            MatchLogEntry(LogLevel.Error, "FiksIO Client is unhealthy, reconnecting.", loggerMock.Object),
            Times.Once
        );
    }

    // TODO: Add test for MessageReceivedHandler (which uses RetrieveInstance and ParseCorrelationId)
    [Fact]
    public Task ExecuteAsync_RegistersMessageReceivedHandler_ExecutesMessageHandler() { }

    [Fact]
    public Task ExecuteAsync_RegistersMessageReceivedHandler_ThrowsOnError() { }

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

    private static (
        Mock<IFiksIOClientFactory> clientFactoryMock,
        List<Mock<KS.Fiks.IO.Client.IFiksIOClient>> createdClients
    ) GetFixIOClientFactoryMock(Action<Mock<KS.Fiks.IO.Client.IFiksIOClient>>? creationCallback = null)
    {
        var clients = new List<Mock<KS.Fiks.IO.Client.IFiksIOClient>>();
        var clientFactoryMock = new Mock<IFiksIOClientFactory>();
        clientFactoryMock
            .Setup(x =>
                x.CreateClient(
                    It.IsAny<KS.Fiks.IO.Client.Configuration.FiksIOConfiguration>(),
                    It.IsAny<Ks.Fiks.Maskinporten.Client.IMaskinportenClient>(),
                    It.IsAny<ILoggerFactory>()
                )
            )
            .ReturnsAsync(() =>
            {
                var clientMock = new Mock<KS.Fiks.IO.Client.IFiksIOClient>();
                creationCallback?.Invoke(clientMock);
                clients.Add(clientMock);

                return clientMock.Object;
            });

        return (clientFactoryMock, clients);
    }
}
