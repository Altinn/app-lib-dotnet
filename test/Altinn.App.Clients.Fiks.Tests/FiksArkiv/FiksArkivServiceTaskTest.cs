using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Send.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivServiceTaskTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AutoSendDecision_IsRespected(bool autoSendDecision)
    {
        // Arrange
        var autoSendDecisionMock = new Mock<IFiksArkivAutoSendDecision>(MockBehavior.Strict);
        var messageHandlerMock = new Mock<IFiksArkivMessageHandler>(MockBehavior.Strict);
        var fiksIOClientMock = new Mock<IFiksIOClient>(MockBehavior.Strict);
        var expectedInvocationTimes = autoSendDecision ? Times.Once() : Times.Never();

        await using var fixture = TestFixture.Create(services =>
            services
                .AddFiksArkiv()
                .CompleteSetup()
                .AddSingleton(autoSendDecisionMock.Object)
                .AddSingleton(fiksIOClientMock.Object)
                .AddSingleton(messageHandlerMock.Object)
        );

        autoSendDecisionMock
            .Setup(x => x.ShouldSend(It.IsAny<string>(), It.IsAny<Instance>()))
            .ReturnsAsync(autoSendDecision)
            .Verifiable(Times.Once);
        messageHandlerMock
            .Setup(x => x.CreateMessageRequest(It.IsAny<string>(), It.IsAny<Instance>()))
            .ReturnsAsync(TestHelpers.GetFiksIOMessageRequest())
            .Verifiable(expectedInvocationTimes);
        messageHandlerMock
            .Setup(x => x.SaveArchiveRecord(It.IsAny<Instance>(), It.IsAny<FiksIOMessageRequest>()))
            .ReturnsAsync(Mock.Of<DataElement>())
            .Verifiable(expectedInvocationTimes);
        fiksIOClientMock
            .Setup(x => x.SendMessage(It.IsAny<FiksIOMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestHelpers.GetFiksIOMessageResponse())
            .Verifiable(expectedInvocationTimes);

        // Act
        await fixture.FiksArkivServiceTask.Execute(string.Empty, new Instance());

        // Assert
        autoSendDecisionMock.Verify();
        messageHandlerMock.Verify();
        fiksIOClientMock.Verify();
    }
}
