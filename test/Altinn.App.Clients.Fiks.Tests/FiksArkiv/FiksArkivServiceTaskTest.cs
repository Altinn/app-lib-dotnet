using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.Platform.Storage.Interface.Models;
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
        var fiksArkivHostMock = new Mock<IFiksArkivHost>(MockBehavior.Strict);
        var expectedInvocationTimes = autoSendDecision ? Times.Once() : Times.Never();

        await using var fixture = TestFixture.Create(services =>
            services
                .AddFiksArkiv()
                .CompleteSetup()
                .AddSingleton(autoSendDecisionMock.Object)
                .AddSingleton(fiksArkivHostMock.Object)
        );

        autoSendDecisionMock
            .Setup(x => x.ShouldSend(It.IsAny<string>(), It.IsAny<Instance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(autoSendDecision)
            .Verifiable(Times.Once);
        fiksArkivHostMock
            .Setup(x =>
                x.GenerateAndSendMessage(
                    It.IsAny<string>(),
                    It.IsAny<Instance>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(TestHelpers.GetFiksIOMessageResponse())
            .Verifiable(expectedInvocationTimes);

        // Act
        await fixture.FiksArkivServiceTask.Execute(string.Empty, new Instance());

        // Assert
        autoSendDecisionMock.Verify();
        fiksArkivHostMock.Verify();
    }
}
