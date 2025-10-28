using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivServiceTaskTest
{
    [Fact]
    public async Task Execute_CallsGenerateAndSendMessage()
    {
        // Arrange
        var fiksArkivHostMock = new Mock<IFiksArkivHost>(MockBehavior.Strict);
        var instanceMutatorMock = new Mock<IInstanceDataMutator>();
        await using var fixture = TestFixture.Create(services =>
            services.AddFiksArkiv().CompleteSetup().AddSingleton(fiksArkivHostMock.Object)
        );

        instanceMutatorMock
            .Setup(x => x.Instance)
            .Returns(
                new Instance
                {
                    Id = "12345/27fde586-4078-4c16-8c5f-ec406f1b17de",
                    Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
                }
            );

        fiksArkivHostMock
            .Setup(x =>
                x.GenerateAndSendMessage(
                    "Task_1",
                    It.Is<Instance>(i => i.Id == "12345/27fde586-4078-4c16-8c5f-ec406f1b17de"),
                    "no.ks.fiks.arkiv.v1.arkivering.arkivmelding.opprett",
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(TestHelpers.GetFiksIOMessageResponse())
            .Verifiable(Times.Once);

        // Act
        var parameters = new ServiceTaskContext { InstanceDataMutator = instanceMutatorMock.Object };
        await fixture.FiksArkivServiceTask.Execute(parameters);

        // Assert
        fiksArkivHostMock.Verify();
    }
}
