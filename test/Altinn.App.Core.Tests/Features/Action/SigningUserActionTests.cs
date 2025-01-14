#nullable disable
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Tests.Internal.Process.TestUtils;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Tests.Features.Action;

public class SigningUserActionTests
{
    private readonly ApplicationMetadata _defaultAppMetadata = new("org/id")
    {
        DataTypes = [new DataType { Id = "model" }],
    };

    private sealed record Fixture(
        Mock<IProfileClient> ProfileClientMock,
        Mock<ISignClient> SignClientMock,
        Mock<IDataClient> DataClientMock,
        Mock<IAppMetadata> AppMetadataMock,
        Mock<IAppResources> AppResourcesMock,
        Mock<IHostEnvironment> HostingEnvMock,
        Mock<ICorrespondenceClient> CorrespondenceClientMock,
        SigningUserAction SigningUserAction
    )
    {
        public static Fixture Create(
            ApplicationMetadata applicationMetadataToReturn,
            UserProfile userProfileToReturn = null,
            PlatformHttpException platformHttpExceptionToThrow = null,
            string testBpmnfilename = "signing-task-process.bpmn"
        )
        {
            IProcessReader processReader = ProcessTestUtils.SetupProcessReader(
                testBpmnfilename,
                Path.Combine("Features", "Action", "TestData")
            );

            var profileClientMock = new Mock<IProfileClient>();
            var signClientMock = new Mock<ISignClient>();
            var dataClientMock = new Mock<IDataClient>();
            var appMetadataMock = new Mock<IAppMetadata>();
            var appResourcesMock = new Mock<IAppResources>();
            var hostingEnvMock = new Mock<IHostEnvironment>();
            var correspondenceClientMock = new Mock<ICorrespondenceClient>();

            appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadataToReturn);
            profileClientMock.Setup(p => p.GetUserProfile(It.IsAny<int>())).ReturnsAsync(userProfileToReturn);
            if (platformHttpExceptionToThrow != null)
            {
                signClientMock
                    .Setup(p => p.SignDataElements(It.IsAny<SignatureContext>()))
                    .ThrowsAsync(platformHttpExceptionToThrow);
            }

            return new Fixture(
                profileClientMock,
                signClientMock,
                dataClientMock,
                appMetadataMock,
                appResourcesMock,
                hostingEnvMock,
                correspondenceClientMock,
                new SigningUserAction(
                    processReader,
                    new NullLogger<SigningUserAction>(),
                    profileClientMock.Object,
                    signClientMock.Object,
                    correspondenceClientMock.Object,
                    dataClientMock.Object,
                    appMetadataMock.Object,
                    appResourcesMock.Object,
                    hostingEnvMock.Object
                )
            );
        }
    }

    [Fact]
    public async Task HandleAction_returns_ok_if_user_is_valid()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };
        var fixture = Fixture.Create(
            applicationMetadataToReturn: _defaultAppMetadata,
            userProfileToReturn: userProfile
        );
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        var expected = new SignatureContext(
            new InstanceIdentifier(instance),
            instance.Process.CurrentTask.ElementId,
            "signature",
            new Signee() { UserId = "1337", PersonNumber = "12345678901" },
            new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
        );
        fixture.SignClientMock.Verify(
            s => s.SignDataElements(It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))),
            Times.Once
        );
        result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_returns_ok_if_no_dataElementSignature_and_optional_datatypes()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };
        var appMetadata = new ApplicationMetadata("org/id")
        {
            // Optional because MinCount == 0
            DataTypes = [new DataType { Id = "model", MinCount = 0 }],
        };
        var fixture = Fixture.Create(applicationMetadataToReturn: appMetadata, userProfileToReturn: userProfile);
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        var expected = new SignatureContext(
            new InstanceIdentifier(instance),
            instance.Process.CurrentTask.ElementId,
            "signature",
            new Signee() { UserId = "1337", PersonNumber = "12345678901" },
            new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
        );
        fixture.SignClientMock.Verify(
            s => s.SignDataElements(It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))),
            Times.Once
        );
        result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_returns_error_when_UserId_not_set_in_context()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };
        var fixture = Fixture.Create(
            applicationMetadataToReturn: _defaultAppMetadata,
            userProfileToReturn: userProfile
        );
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, null);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        var fail = UserActionResult.FailureResult(
            error: new ActionError() { Code = "NoUserId", Message = "User id is missing in token" },
            errorType: ProcessErrorType.Unauthorized
        );
        result.Should().BeEquivalentTo(fail);
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_when_no_dataElementSignature_and_mandatory_datatypes()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };
        var appMetadata = new ApplicationMetadata("org/id")
        {
            // Mandatory because MinCount != 0
            DataTypes =
            [
                new DataType { Id = "not_match", MinCount = 0 },
                new DataType { Id = "not_match_2", MinCount = 1 },
            ],
        };
        var fixture = Fixture.Create(applicationMetadataToReturn: appMetadata, userProfileToReturn: userProfile);
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_If_SignatureDataType_is_null()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };
        var fixture = Fixture.Create(
            applicationMetadataToReturn: _defaultAppMetadata,
            userProfileToReturn: userProfile,
            testBpmnfilename: "signing-task-process-missing-config.bpmn"
        );
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_If_Empty_DataTypesToSign()
    {
        // Arrange
        var userProfile = new UserProfile()
        {
            UserId = 1337,
            Party = new Party() { SSN = "12345678901" },
        };

        var appMetadata = new ApplicationMetadata("org/id") { DataTypes = [] };
        var fixture = Fixture.Create(
            userProfileToReturn: userProfile,
            applicationMetadataToReturn: appMetadata,
            testBpmnfilename: "signing-task-process-empty-datatypes-to-sign.bpmn"
        );
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new() { PartyId = "5000" },
            Process = new() { CurrentTask = new() { ElementId = "Task2" } },
            Data = new()
            {
                new() { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" },
            },
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("test.abc", "-", "-", "test.abc")]
    [InlineData(null, "some-pdf-data", "application/pdf", "some-pdf-data.pdf")]
    [InlineData(null, "foo", "application/xml", "skjemadata_foo.xml")]
    [InlineData(null, "foo", "text/xml", "skjemadata_foo.xml")]
    [InlineData(null, "foo", "application/json", "skjemadata_foo.json")]
    [InlineData(null, "bar", "application/json", "bar.json")]
    [InlineData(null, "baz", "-", "baz")]
    public void GetDataElementFilename_ReturnsExpectedResult(
        string filename,
        string dataTypeName,
        string mimeType,
        string expectedResult
    )
    {
        // Arrange
        var dataElement = new DataElement
        {
            Filename = filename,
            DataType = dataTypeName,
            ContentType = mimeType,
        };
        var appMetadata = new ApplicationMetadata("org/id")
        {
            DataTypes = [new DataType { Id = "foo", AppLogic = new ApplicationLogic() }],
        };

        // Act
        string result = SigningUserAction.GetDataElementFilename(dataElement, appMetadata);

        // Assert
        result.Should().Be(expectedResult);
    }

    private bool AssertSigningContextAsExpected(SignatureContext s1, SignatureContext s2)
    {
        s1.Should().BeEquivalentTo(s2);
        return true;
    }
}
