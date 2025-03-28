using System.Globalization;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Tests.Internal.Process.TestUtils;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Tests.Features.Action;

public class SigningUserActionTests
{
    private static readonly ApplicationMetadata _defaultAppMetadata = new("org/id")
    {
        DataTypes = [new DataType { Id = "model" }],
    };

    private static readonly Instance _defaultInstance = new()
    {
        Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
        InstanceOwner = new InstanceOwner { PartyId = "5000" },
        Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
        Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
    };

    private sealed record Fixture(
        IProcessReader ProcessReader,
        Instance Instance,
        Mock<ISigningReceiptService> SigningReceiptService,
        Mock<IAppMetadata> AppMetadata,
        Mock<ISignClient> SignClient,
        Mock<IInstanceDataMutator> InstanceDataMutatorMock,
        SigningUserAction SigningUserAction
    )
    {
        public static Fixture Create(
            IProcessReader? processReader = null,
            Instance? instance = null,
            string testBpmnFilename = "signing-task-process.bpmn"
        )
        {
            IProcessReader _processReader =
                processReader
                ?? ProcessTestUtils.SetupProcessReader(
                    testBpmnFilename,
                    Path.Combine("Features", "Action", "TestData")
                );
            Instance _instance = instance ?? _defaultInstance;

            var signingReceiptService = new Mock<ISigningReceiptService>();
            var appMetadata = new Mock<IAppMetadata>();
            var signClient = new Mock<ISignClient>();
            var instanceDataMutatorMock = new Mock<IInstanceDataMutator>();

            appMetadata.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(_defaultAppMetadata);
            instanceDataMutatorMock.Setup(x => x.Instance).Returns(_instance);
            signingReceiptService
                .Setup(x =>
                    x.SendSignatureReceipt(
                        It.IsAny<InstanceIdentifier>(),
                        It.IsAny<Signee>(),
                        It.IsAny<IEnumerable<DataElementSignature>>(),
                        It.IsAny<UserActionContext>(),
                        It.IsAny<List<AltinnEnvironmentConfig>>()
                    )
                )
                .Returns(
                    Task.FromResult<SendCorrespondenceResponse?>(
                        new SendCorrespondenceResponse { Correspondences = [] }
                    )
                );

            var signingServiceMock = new Mock<ISigningService>();
            var services = new ServiceCollection();
            services.AddSingleton<ISigningService>(signingServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            return new Fixture(
                _processReader,
                _instance,
                signingReceiptService,
                appMetadata,
                signClient,
                instanceDataMutatorMock,
                new SigningUserAction(
                    serviceProvider,
                    _processReader,
                    signClient.Object,
                    appMetadata.Object,
                    signingReceiptService.Object,
                    new NullLogger<SigningUserAction>()
                )
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
            new UserActionContext(
                fixture.InstanceDataMutatorMock.Object,
                1337,
                authentication: TestAuthentication.GetUserAuthentication(1337)
            )
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

        var userActionContext = new UserActionContext(
            fixture.InstanceDataMutatorMock.Object,
            1337,
            authentication: TestAuthentication.GetUserAuthentication(1337)
        );

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        Assert.Equal(JsonSerializer.Serialize(UserActionResult.SuccessResult()), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task HandleAction_throws_when_SignClient_SignDataElements_throws()
    {
        // Arrange
        var fixture = Fixture.Create();
        fixture
            .SignClient.Setup(x => x.SignDataElements(It.IsAny<SignatureContext>()))
            .ThrowsAsync(new PlatformHttpException(new HttpResponseMessage(), "Failed to sign dataelements"));

        var userActionContext = new UserActionContext(
            fixture.InstanceDataMutatorMock.Object,
            1337,
            authentication: TestAuthentication.GetUserAuthentication(1337)
        );

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);
        var expected = UserActionResult.FailureResult(
            error: new ActionError() { Code = "SignDataElementsFailed", Message = "Failed to sign data elements." },
            errorType: ProcessErrorType.Internal
        );
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result));
        fixture.SignClient.Verify(x => x.SignDataElements(It.IsAny<SignatureContext>()), Times.Once);
    }

    [Theory]
    [ClassData(typeof(TestAuthentication.AllTokens))]
    public async Task HandleAction_returns_ok_if_user_is_valid(TestJwtToken token)
    {
        // Arrange
        var fixture = Fixture.Create();

        var instance = fixture.Instance;
        var signClient = fixture.SignClient;

        // Act
        var result = await fixture.SigningUserAction.HandleAction(
            new UserActionContext(fixture.InstanceDataMutatorMock.Object, 1337, authentication: token.Auth)
        );

        // Assert
        switch (token.Auth)
        {
            case Authenticated.User user:
                {
                    var details = await user.LoadDetails();
                    SignatureContext expected = new(
                        new InstanceIdentifier(instance),
                        instance.Process.CurrentTask.ElementId,
                        "signature",
                        new Signee()
                        {
                            UserId = user.UserId.ToString(CultureInfo.InvariantCulture),
                            PersonNumber = details.SelectedParty.SSN,
                        },
                        new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
                    );
                    signClient.Verify(
                        s =>
                            s.SignDataElements(
                                It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))
                            ),
                        Times.Once
                    );
                    result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
                    signClient.VerifyNoOtherCalls();
                }
                break;
            case Authenticated.SelfIdentifiedUser selfIdentifiedUser:
                {
                    SignatureContext expected = new(
                        new InstanceIdentifier(instance),
                        instance.Process.CurrentTask.ElementId,
                        "signature",
                        new Signee()
                        {
                            UserId = selfIdentifiedUser.UserId.ToString(CultureInfo.InvariantCulture),
                            PersonNumber = null,
                        },
                        new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
                    );
                    signClient.Verify(
                        s =>
                            s.SignDataElements(
                                It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))
                            ),
                        Times.Once
                    );
                    result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
                    signClient.VerifyNoOtherCalls();
                }
                break;
            case Authenticated.SystemUser systemUser:
                {
                    SignatureContext expected = new SignatureContext(
                        new InstanceIdentifier(instance),
                        instance.Process.CurrentTask.ElementId,
                        "signature",
                        new Signee() { SystemUserId = systemUser.SystemUserId[0], OrganisationNumber = null },
                        new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
                    );
                    signClient.Verify(
                        s =>
                            s.SignDataElements(
                                It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))
                            ),
                        Times.Once
                    );
                    result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
                    signClient.VerifyNoOtherCalls();
                }
                break;
            default:
                Assert.Equal(ProcessErrorType.Unauthorized, result.ErrorType);
                break;
        }
    }

    [Fact]
    public async Task HandleAction_returns_ok_if_no_dataElementSignature_and_optional_datatypes()
    {
        // Arrange
        var appMetadata = new ApplicationMetadata("org/id")
        {
            // Optional because MinCount == 0
            DataTypes = [new DataType { Id = "model", MinCount = 0 }],
        };

        var fixture = Fixture.Create();

        fixture.AppMetadata.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(appMetadata);

        var instance = fixture.Instance;
        var signClientMock = fixture.SignClient;

        // Act
        var result = await fixture.SigningUserAction.HandleAction(
            new UserActionContext(instance, 1337, authentication: TestAuthentication.GetUserAuthentication(1337))
        );

        // Assert
        SignatureContext expected = new(
            new InstanceIdentifier(instance),
            instance.Process.CurrentTask.ElementId,
            "signature",
            new Signee() { UserId = "1337", PersonNumber = "12345678901" },
            new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
        );
        signClientMock.Verify(
            s => s.SignDataElements(It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expected))),
            Times.Once
        );
        result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
        signClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_when_no_dataElementSignature_and_mandatory_datatypes()
    {
        // Arrange
        var appMetadata = new ApplicationMetadata("org/id")
        {
            // Mandatory because MinCount != 0
            DataTypes =
            [
                new DataType { Id = "not_match", MinCount = 0 },
                new DataType { Id = "not_match_2", MinCount = 1 },
            ],
        };
        var fixture = Fixture.Create();
        fixture.AppMetadata.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(appMetadata);

        var instance = fixture.Instance;

        var userActionContext = new UserActionContext(
            instance,
            1337,
            authentication: TestAuthentication.GetUserAuthentication(1337, applicationMetadata: appMetadata)
        );

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SignClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_if_SignatureDataType_is_missing_from_bpmn_config()
    {
        // Arrange
        var fixture = Fixture.Create(testBpmnFilename: "signing-task-process-missing-config.bpmn");
        var instance = fixture.Instance;
        var signClientMock = fixture.SignClient;

        var userActionContext = new UserActionContext(
            instance,
            1337,
            authentication: TestAuthentication.GetUserAuthentication(1337)
        );

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        signClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_throws_ApplicationConfigException_if_empty_DataTypesToSign_in_bpmn_config()
    {
        // Arrange
        var fixture = Fixture.Create(testBpmnFilename: "signing-task-process-empty-datatypes-to-sign.bpmn");
        var instance = fixture.Instance;
        var signClientMock = fixture.SignClient;

        var userActionContext = new UserActionContext(
            instance,
            1337,
            authentication: TestAuthentication.GetUserAuthentication(1337, applicationMetadata: _defaultAppMetadata)
        );

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        signClientMock.VerifyNoOtherCalls();
    }

    private bool AssertSigningContextAsExpected(SignatureContext s1, SignatureContext s2)
    {
        s1.Should().BeEquivalentTo(s2);
        return true;
    }
}

public class SigningUserActionHandleOnBehalfOfTests
{
    // // Helper: Creates a dummy Instance with the provided instance owner organisation number.
    private Instance CreateDummyInstance(string instanceOwnerOrg)
    {
        return new Instance
        {
            Id = "500000/12345678-1234-1234-1234-123456789012",
            InstanceOwner = new InstanceOwner { PartyId = "5000", OrganisationNumber = instanceOwnerOrg },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task1" } },
        };
    }

    // // Helper: Creates a SigningUserAction with a mocked ISigningService.
    private static SigningUserAction CreateSigningUserAction(out Mock<ISigningService> signingServiceMock)
    {
        signingServiceMock = new Mock<ISigningService>();
        var services = new ServiceCollection();
        services.AddSingleton<ISigningService>(signingServiceMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Other dependencies are dummies since HandleOnBehalfOf doesn't use them.
        IProcessReader dummyProcessReader = Mock.Of<IProcessReader>();
        ISignClient dummySignClient = Mock.Of<ISignClient>();
        var dummyAppMetadata = Mock.Of<IAppMetadata>();
        var dummySigningReceiptService = Mock.Of<ISigningReceiptService>();

        return new SigningUserAction(
            serviceProvider,
            dummyProcessReader,
            dummySignClient,
            dummyAppMetadata,
            dummySigningReceiptService,
            new NullLogger<SigningUserAction>()
        );
    }

    [Fact]
    public async Task HandleOnBehalfOf_ReturnsTrue_When_OnBehalfOf_Equals_InstanceOwner()
    {
        // Arrange:
        // If the context's OnBehalfOf equals the instance owner's organisation number,
        // the method should return true immediately without calling the signing service.
        string ownerOrg = "12345";
        Instance instance = CreateDummyInstance(ownerOrg);

        var userId = 1337;
        var dataMutator = new Mock<IInstanceDataMutator>();
        dataMutator.Setup(dm => dm.Instance).Returns(instance);
        var context = new UserActionContext(
            dataMutator.Object,
            userId,
            authentication: TestAuthentication.GetUserAuthentication(userId),
            onBehalfOf: ownerOrg
        );

        var signatureConfig = new AltinnSignatureConfiguration();
        var action = CreateSigningUserAction(out var signingServiceMock);

        // Act:
        bool result = await action.HandleOnBehalfOf(context, signatureConfig);

        // Assert:
        result.Should().BeTrue();
        signingServiceMock.Verify(
            s =>
                s.GetAuthorizedOrganisationSignees(
                    It.IsAny<IInstanceDataMutator>(),
                    It.IsAny<AltinnSignatureConfiguration>(),
                    It.IsAny<int>()
                ),
            Times.Never,
            "the instance owner check should bypass any call to GetAuthorizedOrganisationSignees"
        );
    }

    [Fact]
    public async Task HandleOnBehalfOf_ReturnsFalse_When_Authentication_Is_Unsupported()
    {
        // Arrange:
        // For an unsupported authentication type (e.g. Authenticated.SystemUser), the switch returns null.
        string onBehalfOrg = "67890";
        Instance instance = CreateDummyInstance("12345"); // instance owner is different
        var auth = TestAuthentication.GetSystemUserAuthentication();
        // Although userId is not used for unsupported types, we need to supply a dummy value.
        var dummyUserId = 0;
        var dataMutator = new Mock<IInstanceDataMutator>();
        dataMutator.Setup(dm => dm.Instance).Returns(instance);
        var context = new UserActionContext(
            dataMutator.Object,
            dummyUserId,
            authentication: auth,
            onBehalfOf: onBehalfOrg
        );

        var signatureConfig = new AltinnSignatureConfiguration();
        var action = CreateSigningUserAction(out var signingServiceMock);

        // Act:
        bool result = await action.HandleOnBehalfOf(context, signatureConfig);

        // Assert:
        result.Should().BeFalse();
        signingServiceMock.Verify(
            s =>
                s.GetAuthorizedOrganisationSignees(
                    It.IsAny<IInstanceDataMutator>(),
                    It.IsAny<AltinnSignatureConfiguration>(),
                    It.IsAny<int>()
                ),
            Times.Never,
            "unsupported authentication should return false without calling the signing service"
        );
    }

    [Fact]
    public async Task HandleOnBehalfOf_ReturnsFalse_When_User_Is_Not_Authorized()
    {
        // Arrange:
        // For an Authenticated.User with a valid user id, but the signing service returns a list
        // that does not include the provided OnBehalfOf value.
        string onBehalfOrg = "67890";
        Instance instance = CreateDummyInstance("12345"); // instance owner is different
        var userId = 100;
        var dataMutator = new Mock<IInstanceDataMutator>();
        dataMutator.Setup(dm => dm.Instance).Returns(instance);
        var context = new UserActionContext(
            dataMutator.Object,
            userId,
            authentication: TestAuthentication.GetUserAuthentication(userId),
            onBehalfOf: onBehalfOrg
        );

        var signatureConfig = new AltinnSignatureConfiguration();
        var action = CreateSigningUserAction(out var signingServiceMock);

        signingServiceMock
            .Setup(s => s.GetAuthorizedOrganisationSignees(dataMutator.Object, signatureConfig, userId))
            .ReturnsAsync(
                new List<OrganisationSignee>
                {
                    new OrganisationSignee
                    {
                        OrgNumber = "111111111",
                        OrgName = "TestOrg",
                        OrgParty = new Platform.Register.Models.Party { PartyId = 123 },
                    },
                }
            );

        // Act:
        bool result = await action.HandleOnBehalfOf(context, signatureConfig);

        // Assert:
        result.Should().BeFalse();
        signingServiceMock.Verify(
            s => s.GetAuthorizedOrganisationSignees(dataMutator.Object, signatureConfig, userId),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleOnBehalfOf_ReturnsTrue_When_User_Is_Authorized()
    {
        // Arrange:
        // For an Authenticated.User with a valid user id and the signing service returns a list
        // that includes the provided OnBehalfOf value.
        string onBehalfOrg = "67890";
        Instance instance = CreateDummyInstance(instanceOwnerOrg: "12345"); // instance owner different
        var userId = 200;
        var dataMutator = new Mock<IInstanceDataMutator>();
        dataMutator.Setup(d => d.Instance).Returns(instance);
        var context = new UserActionContext(
            dataMutator.Object,
            userId,
            authentication: TestAuthentication.GetUserAuthentication(userId),
            onBehalfOf: onBehalfOrg
        );

        var signatureConfig = new AltinnSignatureConfiguration();
        var action = CreateSigningUserAction(out var signingServiceMock);

        signingServiceMock
            .Setup(s => s.GetAuthorizedOrganisationSignees(dataMutator.Object, signatureConfig, 200))
            .ReturnsAsync(
                [
                    new OrganisationSignee
                    {
                        OrgNumber = onBehalfOrg,
                        OrgName = "TestOrg",
                        OrgParty = new Platform.Register.Models.Party { PartyId = 123 },
                    },
                ]
            );

        // Act:
        bool result = await action.HandleOnBehalfOf(context, signatureConfig);

        // Assert:
        result.Should().BeTrue();
        signingServiceMock.Verify(
            s => s.GetAuthorizedOrganisationSignees(dataMutator.Object, signatureConfig, 200),
            Times.Once
        );
    }
}
