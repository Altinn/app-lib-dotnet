using System.Linq.Expressions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Send;
using KS.Fiks.IO.Crypto.Models;
using Ks.Fiks.Maskinporten.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using MessageReceivedCallback = System.Func<
    Altinn.App.Clients.Fiks.FiksIO.Models.FiksIOReceivedMessage,
    System.Threading.Tasks.Task
>;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivEventServiceTest
{
    [Fact]
    public async Task ExecuteAsync_StopsWhenCancellationRequested()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FiksArkivEventService>>();
        var (clientFactoryMock, createdClients) = GetFixIOClientFactoryMock();
        using var cts = new CancellationTokenSource();

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
            services.AddTransient<IFiksIOClientFactory>(sp => clientFactoryMock.Object);
        });

        // Act
        await fixture.FiksArkivEventService.StartAsync(cts.Token);
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

        // fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(TestHelpers.GetHttpClientWithMockedHandlerFactory());

        // Act
        await fixture.FiksArkivEventService.StartAsync(CancellationToken.None);
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

    [Fact]
    public async Task ExecuteAsync_RegistersMessageReceivedHandler_ExecutesMessageHandler()
    {
        // Arrange
        var fiksIOClientMock = new Mock<IFiksIOClient>();
        var fiksArkivMessageHandlerMock = new Mock<IFiksArkivMessageHandler>();
        var fiksArkivInstanceClientMock = new Mock<IFiksArkivInstanceClient>();
        var mottattMeldingMock = new Mock<IMottattMelding>();
        var svarSenderMock = new Mock<ISvarSender>();
        var messageId = Guid.NewGuid();
        var payload = new FiksIOReceivedMessage(
            new MottattMeldingArgs(mottattMeldingMock.Object, svarSenderMock.Object)
        );
        MessageReceivedCallback? messageReceivedCallback = null;
        FiksIOReceivedMessage? forwardedMessage = null;
        Instance? forwardedInstance = null;
        Instance sourceInstance = new() { Id = $"12345/{Guid.NewGuid()}", AppId = "ttd/unit-testing" };
        InstanceIdentifier? receivedInstanceIdentifier = null;

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<IFiksIOClient>(sp => fiksIOClientMock.Object);
            services.AddTransient<IFiksArkivMessageHandler>(sp => fiksArkivMessageHandlerMock.Object);
            services.AddTransient<IFiksArkivInstanceClient>(sp => fiksArkivInstanceClientMock.Object);
        });

        fiksIOClientMock
            .Setup(x => x.OnMessageReceived(It.IsAny<MessageReceivedCallback>()))
            .Returns(
                (MessageReceivedCallback callback) =>
                {
                    messageReceivedCallback = callback;
                    return Task.CompletedTask;
                }
            )
            .Verifiable(Times.Once);

        fiksArkivMessageHandlerMock
            .Setup(x => x.HandleReceivedMessage(It.IsAny<Instance>(), It.IsAny<FiksIOReceivedMessage>()))
            .Returns(
                (Instance instance, FiksIOReceivedMessage message) =>
                {
                    forwardedMessage = message;
                    forwardedInstance = instance;
                    return Task.CompletedTask;
                }
            )
            .Verifiable(Times.Once);
        fiksArkivInstanceClientMock
            .Setup(x => x.GetInstance(It.IsAny<InstanceIdentifier>()))
            .ReturnsAsync(
                (InstanceIdentifier instanceIdentifier) =>
                {
                    receivedInstanceIdentifier = instanceIdentifier;
                    return sourceInstance;
                }
            )
            .Verifiable(Times.Once);

        mottattMeldingMock.Setup(x => x.HasPayload).Returns(true);
        mottattMeldingMock.Setup(x => x.DecryptedPayloads).ReturnsAsync([new StreamPayload(Stream.Null, "dummy.txt")]);
        mottattMeldingMock.Setup(x => x.MeldingId).Returns(messageId);
        mottattMeldingMock
            .Setup(x => x.KlientKorrelasjonsId)
            .Returns(sourceInstance.GetInstanceUrl(new GeneralSettings()).ToUrlSafeBase64());

        svarSenderMock.Setup(x => x.AckAsync()).Verifiable(Times.Once);

        // Act
        await fixture.FiksArkivEventService.StartAsync(CancellationToken.None);
        await messageReceivedCallback!.Invoke(payload);

        // Assert
        Assert.NotNull(forwardedMessage);
        Assert.NotNull(forwardedInstance);
        Assert.NotNull(receivedInstanceIdentifier);
        Assert.Equal(sourceInstance, forwardedInstance);
        Assert.Equal(messageId, forwardedMessage.Message.MessageId);
        Assert.Equivalent(new InstanceIdentifier(sourceInstance).InstanceGuid, receivedInstanceIdentifier.InstanceGuid);

        fiksIOClientMock.Verify();
        fiksArkivMessageHandlerMock.Verify();
        fiksArkivInstanceClientMock.Verify();
        svarSenderMock.Verify();
        svarSenderMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Staging", true)]
    [InlineData("Production", false)]
    [InlineData("Unknown", true)]
    public async Task MessageReceivedHandler_HandlesErrorIfThrown(string environment, bool shouldAck)
    {
        // Arrange
        var fiksArkivMessageHandlerMock = new Mock<IFiksArkivMessageHandler>();
        var fiksArkivInstanceClientMock = new Mock<IFiksArkivInstanceClient>();
        var loggerMock = new Mock<ILogger<FiksArkivEventService>>();
        var svarSenderMock = new Mock<ISvarSender>();
        var payload = new FiksIOReceivedMessage(
            new MottattMeldingArgs(new Mock<IMottattMelding>().Object, svarSenderMock.Object)
        );

        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksArkiv();
            services.AddTransient<ILogger<FiksArkivEventService>>(sp => loggerMock.Object);
            services.AddTransient<IFiksArkivMessageHandler>(sp => fiksArkivMessageHandlerMock.Object);
        });

        fiksArkivMessageHandlerMock
            .Setup(x => x.HandleReceivedMessage(It.IsAny<Instance>(), It.IsAny<FiksIOReceivedMessage>()))
            .ThrowsAsync(new InvalidOperationException());

        fiksArkivInstanceClientMock
            .Setup(x => x.GetInstance(It.IsAny<InstanceIdentifier>()))
            .ReturnsAsync(new Instance());

        fixture.HostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(environment);

        // Act
        await fixture.FiksArkivEventService.MessageReceivedHandler(payload);

        // Assert
        svarSenderMock.Verify(x => x.AckAsync(), shouldAck ? Times.Once : Times.Never);
        loggerMock.Verify(MatchLogEntry(LogLevel.Information, "received message", loggerMock.Object), Times.Once);
        loggerMock.Verify(MatchLogEntry(LogLevel.Error, "failed with error", loggerMock.Object), Times.Once);
    }

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
