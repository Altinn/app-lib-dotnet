using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Tests.Internal.Process.TestUtils;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Altinn.App.Core.Tests.Features.Action;

public class SigningUserActionTests
{
    private sealed record Fixture(
        IProcessReader ProcessReader,
        Instance Instance,
        Mock<ISigningService> SigningServiceMock,
        Mock<IInstanceDataMutator> InstanceDataMutatorMock,
        SigningUserAction SigningUserAction
    )
    {
        public static Fixture Create(
            IProcessReader? processReader = null,
            string testBpmnFilename = "signing-task-process.bpmn"
        )
        {
            IProcessReader _processReader =
                processReader
                ?? ProcessTestUtils.SetupProcessReader(
                    testBpmnFilename,
                    Path.Combine("Features", "Action", "TestData")
                );

            var signingServiceMock = new Mock<ISigningService>();
            var instanceDataMutatorMock = new Mock<IInstanceDataMutator>();
            var instance = new Instance
            {
                Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
                InstanceOwner = new InstanceOwner { PartyId = "5000" },
                Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
                Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
            };

            return new Fixture(
                _processReader,
                instance,
                signingServiceMock,
                instanceDataMutatorMock,
                new SigningUserAction(_processReader, signingServiceMock.Object, new NullLogger<SigningUserAction>())
            );
        }
    }

    [Fact]
    public async Task HandleAction_returns_failure_when_UserActionContext_UserId_is_null()
    {
        // Arrange
        var fixture = Fixture.Create();

        // Act
        var result = await fixture.SigningUserAction.HandleAction(
            new UserActionContext(fixture.InstanceDataMutatorMock.Object, null)
        );

        // Assert
        var expected = UserActionResult.FailureResult(
            error: new ActionError { Code = "NoUserId", Message = "User id is missing in token" },
            errorType: ProcessErrorType.Unauthorized
        );

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task HandleAction_returns_failure_when_processReader_GetFlowElement_is_null()
    {
        // Arrange
        var processReaderMock = new Mock<IProcessReader>();
        processReaderMock.Setup(x => x.GetFlowElement(It.IsAny<string>())).Returns(null as ProcessTask);
        var fixture = Fixture.Create(processReaderMock.Object);
        fixture.InstanceDataMutatorMock.Setup(x => x.Instance).Returns(fixture.Instance);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(
            new UserActionContext(fixture.InstanceDataMutatorMock.Object, 1337)
        );

        // Assert
        var expected = UserActionResult.FailureResult(
            new ActionError { Code = "NoProcessTask", Message = "Current task is not a process task." }
        );

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task HandleAction_returns_ok_if_SigningService_Sign_does_not_throw()
    {
        // Arrange
        var fixture = Fixture.Create();

        fixture.InstanceDataMutatorMock.Setup(x => x.Instance).Returns(fixture.Instance);
        var userActionContext = new UserActionContext(fixture.InstanceDataMutatorMock.Object, 1337);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        Assert.Equal(JsonSerializer.Serialize(UserActionResult.SuccessResult()), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task HandleAction_throws_when_SigningService_Sign_throws()
    {
        // Arrange
        var fixture = Fixture.Create();
        fixture.InstanceDataMutatorMock.Setup(x => x.Instance).Returns(fixture.Instance);
        var userActionContext = new UserActionContext(fixture.InstanceDataMutatorMock.Object, 1337);

        fixture
            .SigningServiceMock.Setup(x => x.Sign(It.IsAny<UserActionContext>(), It.IsAny<ProcessTask>()))
            .ThrowsAsync(new ApplicationConfigException());

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SigningServiceMock.Verify(
            x => x.Sign(It.IsAny<UserActionContext>(), It.IsAny<ProcessTask>()),
            Times.Once
        );
    }
}
