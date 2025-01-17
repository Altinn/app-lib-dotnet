#nullable disable
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
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
    private static readonly ApplicationMetadata _defaultAppMetadata = new("ttd/test-app")
    {
        Org = "ttd",
        DataTypes = [new DataType { Id = "model", AppLogic = new ApplicationLogic() }],
        Title = new Dictionary<string, string>
        {
            [LanguageConst.Nb] = "App name from appmetadata NB",
            [LanguageConst.Nn] = "App name from appmetadata NN",
            [LanguageConst.En] = "App name from appmetadata EN",
        },
    };

    private static readonly AltinnCdnOrgDetails _defaultAltinnCdnTtdDetails = new()
    {
        Orgnr = "991825827",
        Name = new AltinnCdnOrgName
        {
            En = "Org name from cdndetails EN",
            Nb = "Org name from cdndetails NB",
            Nn = "Org name from cdndetails NN",
        },
    };

    private static readonly UserProfile _defaultUserProfile = new()
    {
        UserId = 1337,
        Party = new Party { SSN = "13896396174" },
    };

    private sealed record Fixture(
        Mock<IProfileClient> ProfileClientMock,
        Mock<ISignClient> SignClientMock,
        Mock<IDataClient> DataClientMock,
        Mock<IAppMetadata> AppMetadataMock,
        Mock<IAppResources> AppResourcesMock,
        Mock<IHostEnvironment> HostingEnvMock,
        Mock<ICorrespondenceClient> CorrespondenceClientMock,
        IProcessReader ProcessReader,
        SigningUserAction SigningUserAction
    )
    {
        public static Fixture Create(
            ApplicationMetadata applicationMetadata = null,
            UserProfile userProfile = null,
            PlatformHttpException platformHttpExceptionToThrow = null,
            string testBpmnFilename = "signing-task-process.bpmn"
        )
        {
            IProcessReader processReader = ProcessTestUtils.SetupProcessReader(
                testBpmnFilename,
                Path.Combine("Features", "Action", "TestData")
            );

            applicationMetadata ??= _defaultAppMetadata;
            userProfile ??= _defaultUserProfile;

            var profileClientMock = new Mock<IProfileClient>();
            var signClientMock = new Mock<ISignClient>();
            var dataClientMock = new Mock<IDataClient>();
            var appMetadataMock = new Mock<IAppMetadata>();
            var appResourcesMock = new Mock<IAppResources>();
            var hostingEnvMock = new Mock<IHostEnvironment>();
            var correspondenceClientMock = new Mock<ICorrespondenceClient>();

            appMetadataMock.Setup(m => m.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);
            profileClientMock.Setup(p => p.GetUserProfile(It.IsAny<int>())).ReturnsAsync(userProfile);
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
                processReader,
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
        var fixture = Fixture.Create();
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        var expectedResult = new SignatureContext(
            new InstanceIdentifier(instance),
            instance.Process.CurrentTask.ElementId,
            "signature",
            new Signee()
            {
                UserId = _defaultUserProfile.UserId.ToString(),
                PersonNumber = _defaultUserProfile.Party.SSN,
            },
            new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
        );
        fixture.SignClientMock.Verify(
            s => s.SignDataElements(It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expectedResult))),
            Times.Once
        );
        fixture.CorrespondenceClientMock.Verify(
            c => c.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
        fixture.SignClientMock.VerifyNoOtherCalls();
        fixture.CorrespondenceClientMock.VerifyNoOtherCalls();
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
        var fixture = Fixture.Create(applicationMetadata: appMetadata);
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        var result = await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        var expectedResult = new SignatureContext(
            new InstanceIdentifier(instance),
            instance.Process.CurrentTask.ElementId,
            "signature",
            new Signee()
            {
                UserId = _defaultUserProfile.UserId.ToString(),
                PersonNumber = _defaultUserProfile.Party.SSN,
            },
            new DataElementSignature("a499c3ef-e88a-436b-8650-1c43e5037ada")
        );
        fixture.SignClientMock.Verify(
            s => s.SignDataElements(It.Is<SignatureContext>(sc => AssertSigningContextAsExpected(sc, expectedResult))),
            Times.Once
        );
        result.Should().BeEquivalentTo(UserActionResult.SuccessResult());
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_returns_error_when_UserId_not_set_in_context()
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = new Instance
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
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
        var appMetadata = new ApplicationMetadata("org/id")
        {
            // Mandatory because MinCount != 0
            DataTypes =
            [
                new DataType { Id = "not_match", MinCount = 0 },
                new DataType { Id = "not_match_2", MinCount = 1 },
            ],
        };
        var fixture = Fixture.Create(applicationMetadata: appMetadata);
        var instance = new Instance
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
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
        var fixture = Fixture.Create(testBpmnFilename: "signing-task-process-missing-config.bpmn");
        var instance = new Instance
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
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
        var appMetadata = new ApplicationMetadata("org/id") { DataTypes = [] };
        var fixture = Fixture.Create(
            applicationMetadata: appMetadata,
            testBpmnFilename: "signing-task-process-empty-datatypes-to-sign.bpmn"
        );
        var instance = new Instance
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
        };
        var userActionContext = new UserActionContext(instance, 1337);

        // Act
        await Assert.ThrowsAsync<ApplicationConfigException>(
            async () => await fixture.SigningUserAction.HandleAction(userActionContext)
        );
        fixture.SignClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAction_SendsCorrespondenceReceipt()
    {
        // Arrange
        var instance = new Instance
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner { PartyId = "5000" },
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task2" } },
            Data = [new DataElement { Id = "a499c3ef-e88a-436b-8650-1c43e5037ada", DataType = "Model" }],
        };
        var userActionContext = new UserActionContext(instance, 1337);

        var fixture = Fixture.Create();
        fixture.HostingEnvMock.Setup(x => x.EnvironmentName).Returns("Testing");

        var altinnCdnClient = new Mock<IAltinnCdnClient>();
        altinnCdnClient
            .Setup(x => x.GetOrgs(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AltinnCdnOrgs
                {
                    Orgs = new Dictionary<string, AltinnCdnOrgDetails> { { "ttd", _defaultAltinnCdnTtdDetails } },
                }
            );

        userActionContext.AltinnCdnClient = altinnCdnClient.Object;

        fixture
            .DataClientMock.Setup(x =>
                x.GetDataBytes(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()
                )
            )
            .ReturnsAsync([1, 2, 3]);

        // Act
        await fixture.SigningUserAction.HandleAction(userActionContext);

        // Assert
        fixture.CorrespondenceClientMock.Verify(
            x => x.Send(It.IsAny<SendCorrespondencePayload>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        fixture.CorrespondenceClientMock.VerifyNoOtherCalls();
        altinnCdnClient.Verify(x => x.GetOrgs(It.IsAny<CancellationToken>()), Times.Once);
        altinnCdnClient.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("-", "correspondenceResourceGlobal")]
    [InlineData("Development", "correspondenceResourceLocal")]
    [InlineData("Staging", "correspondenceResourceTt02")]
    [InlineData("Production", "correspondenceResourceProd")]
    public async Task GetCorrespondenceHeaders_ReturnsExpectedHeaders(string environment, string expectedResource)
    {
        // Arrange
        var signee = new Signee { PersonNumber = "12345678901" };
        var fixture = Fixture.Create();
        var signatureConfig = (fixture.ProcessReader.GetFlowElement("Task2") as ProcessTask)
            ?.ExtensionElements
            ?.TaskExtension
            ?.SignatureConfiguration;
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(x => x.EnvironmentName).Returns(environment);

        var altinnCdnClient = new Mock<IAltinnCdnClient>();
        altinnCdnClient
            .Setup(x => x.GetOrgs(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AltinnCdnOrgs
                {
                    Orgs = new Dictionary<string, AltinnCdnOrgDetails> { { "ttd", _defaultAltinnCdnTtdDetails } },
                }
            );

        // Act
        var result = await SigningUserAction.GetCorrespondenceHeaders(
            signee,
            _defaultAppMetadata,
            signatureConfig,
            hostEnvironment.Object,
            altinnCdnClient.Object
        );

        // Assert
        result.resource.Should().Be(expectedResource);
        result.senderOrgNumber.Should().Be(_defaultAltinnCdnTtdDetails.Orgnr);
        result.senderDetails.Name!.En.Should().Be(_defaultAltinnCdnTtdDetails.Name!.En);
        result.recipient.Should().Be("12345678901");
    }

    [Fact]
    public async Task GetCorrespondenceContent_ReturnsDefaultContent_WhenTextResourceNotFound()
    {
        // Arrange
        var fixture = Fixture.Create();
        var context = new UserActionContext(new Instance(), 1337);

        // Act
        var result = await fixture.SigningUserAction.GetCorrespondenceContent(
            context,
            _defaultAppMetadata,
            _defaultAltinnCdnTtdDetails
        );

        // Assert
        result.Title.Should().Be("App name from appmetadata NB: Signeringen er bekreftet");
        result.Summary.Should().Be("Du har signert for App name from appmetadata NB.");
        result
            .Body.Should()
            .MatchRegex(@"^Dokumentene du har signert .+ kan du kontakte Org name from cdndetails NB\.$");
    }

    [Fact]
    public async Task GetCorrespondenceContent_ReturnsCustomContent_WhenFullTextResourceFound()
    {
        // Arrange
        var fixture = Fixture.Create();
        var context = new UserActionContext(
            new Instance { AppId = "doesn't/matter" },
            1337,
            language: LanguageConst.En
        );
        var textResource = new TextResource
        {
            Language = LanguageConst.En,
            Resources =
            [
                new TextResourceElement { Id = "signing.receipt_title", Value = "Custom Title" },
                new TextResourceElement { Id = "signing.receipt_summary", Value = "Custom Summary" },
                new TextResourceElement { Id = "signing.receipt_body", Value = "Custom Body" },
            ],
        };

        fixture
            .AppResourcesMock.Setup(x => x.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(textResource);

        // Act
        var result = await fixture.SigningUserAction.GetCorrespondenceContent(
            context,
            _defaultAppMetadata,
            _defaultAltinnCdnTtdDetails
        );

        // Assert
        result.Title.Should().Be("Custom Title");
        result.Summary.Should().Be("Custom Summary");
        result.Body.Should().Be("Custom Body");
    }

    [Fact]
    public async Task GetCorrespondenceContent_ReturnsCustomContent_WhenPartialTextResourceFound()
    {
        // Arrange
        var fixture = Fixture.Create();
        var context = new UserActionContext(
            new Instance { AppId = "doesn't/matter" },
            1337,
            language: LanguageConst.En
        );
        var textResource = new TextResource
        {
            Language = LanguageConst.En,
            Resources =
            [
                new TextResourceElement { Id = "appName", Value = "App name from textresource EN" },
                new TextResourceElement { Id = "signing.receipt_title", Value = "Custom Title" },
                new TextResourceElement { Id = "signing.receipt_body", Value = "Custom Body" },
            ],
        };

        fixture
            .AppResourcesMock.Setup(x => x.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(textResource);

        // Act
        var result = await fixture.SigningUserAction.GetCorrespondenceContent(
            context,
            _defaultAppMetadata,
            _defaultAltinnCdnTtdDetails
        );

        // Assert
        result.Title.Should().Be("Custom Title");
        result.Summary.Should().Be("Du har signert for App name from textresource EN.");
        result.Body.Should().Be("Custom Body");
    }

    [Fact]
    public async Task GetCorrespondenceAttachments_ReturnsExpectedAttachments()
    {
        // Arrange
        var appMetadata = new ApplicationMetadata("org/id")
        {
            DataTypes =
            [
                new DataType { Id = "model", AppLogic = new ApplicationLogic() },
                new DataType { Id = "attachmentToSign" },
            ],
        };
        var modelGuid = Guid.NewGuid();
        var attachmentGuid = Guid.NewGuid();
        var modelDataElement = new DataElement
        {
            Id = modelGuid.ToString(),
            DataType = "model",
            ContentType = "application/xml",
        };
        var attachmentDataElement = new DataElement
        {
            Id = attachmentGuid.ToString(),
            DataType = "attachmentToSign",
            ContentType = "application/pdf",
            Filename = "custom-filename-stored-in-data-element.pdf",
        };
        var context = new UserActionContext(
            new Instance
            {
                Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
                InstanceOwner = new InstanceOwner { PartyId = "5000" },
                Data =
                [
                    new DataElement
                    {
                        Id = Guid.NewGuid().ToString(),
                        DataType = "unrelatedDataType",
                        ContentType = "application/pdf",
                    },
                    modelDataElement,
                    attachmentDataElement,
                ],
            },
            1337
        );
        var instanceIdentifier = new InstanceIdentifier(context.Instance);
        var dataElementSignatures = SigningUserAction.GetDataElementSignatures(
            context.Instance.Data,
            appMetadata.DataTypes
        );

        var fixture = Fixture.Create(applicationMetadata: appMetadata);
        fixture
            .DataClientMock.Setup(x =>
                x.GetDataBytes(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Guid>(), modelGuid)
            )
            .ReturnsAsync([1, 2, 3]);
        fixture
            .DataClientMock.Setup(x =>
                x.GetDataBytes(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    attachmentGuid
                )
            )
            .ReturnsAsync([4, 5, 6]);

        // Act
        var result = (
            await SigningUserAction.GetCorrespondenceAttachments(
                instanceIdentifier,
                dataElementSignatures,
                appMetadata,
                context,
                fixture.DataClientMock.Object
            )
        ).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("skjemadata_model.xml");
        result[0].Filename.Should().Be("skjemadata_model.xml");
        result[0].SendersReference.Should().Be(modelGuid.ToString());
        result[0].DataType.Should().Be(modelDataElement.ContentType);
        result[0].Data.Span.SequenceEqual(new ReadOnlyMemory<byte>([1, 2, 3]).Span).Should().BeTrue();
        result[1].Name.Should().Be(attachmentDataElement.Filename);
        result[1].Filename.Should().Be(attachmentDataElement.Filename);
        result[1].SendersReference.Should().Be(attachmentGuid.ToString());
        result[1].DataType.Should().Be(attachmentDataElement.ContentType);
        result[1].Data.Span.SequenceEqual(new ReadOnlyMemory<byte>([4, 5, 6]).Span).Should().BeTrue();
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

    private static bool AssertSigningContextAsExpected(SignatureContext s1, SignatureContext s2)
    {
        s1.Should().BeEquivalentTo(s2);
        return true;
    }
}
