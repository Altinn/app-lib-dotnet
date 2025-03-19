using System.Text.Json;
using Altinn.App.Api.Controllers;
using Altinn.App.Api.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using SigneeContext = Altinn.App.Core.Features.Signing.Models.SigneeContext;
using SigneeContextState = Altinn.App.Core.Features.Signing.Models.SigneeState;

namespace Altinn.App.Api.Tests.Controllers;

public class SigningControllerTests
{
    private readonly Mock<IInstanceClient> _instanceClientMock = new();
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly Mock<ILogger<SigningController>> _loggerMock = new();
    private readonly Mock<ISigningService> _signingServiceMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly Mock<IAppMetadata> _applicationMetadataMock = new();
    private readonly Mock<IAppModel> _appModelMock = new();
    private readonly Mock<IAppResources> _appResourcesMock = new();
    private readonly ServiceCollection _serviceCollection = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    private readonly AltinnTaskExtension _altinnTaskExtension = new()
    {
        SignatureConfiguration = new AltinnSignatureConfiguration
        {
            DataTypesToSign = ["dataTypeToSign"],
            SignatureDataType = "signatureDataType",
            SigneeProviderId = "signeeProviderId",
            SigneeStatesDataTypeId = "signeeStatesDataTypeId",
            SigningPdfDataType = "signingPdfDataType",
            CorrespondenceResources = [],
            RunDefaultValidator = true,
        },
    };

    public SigningControllerTests()
    {
        _serviceCollection.AddTransient<ModelSerializationService>();
        _serviceCollection.AddTransient<InstanceDataUnitOfWorkInitializer>();
        _serviceCollection.AddTransient<SigningController>();
        _serviceCollection.AddSingleton(Options.Create(new FrontEndSettings()));
        _serviceCollection.AddSingleton(_instanceClientMock.Object);
        _serviceCollection.AddSingleton(_signingServiceMock.Object);
        _serviceCollection.AddSingleton(_appModelMock.Object);
        _serviceCollection.AddSingleton(_dataClientMock.Object);
        _serviceCollection.AddSingleton(_applicationMetadataMock.Object);
        _serviceCollection.AddSingleton(_appResourcesMock.Object);
        _serviceCollection.AddSingleton(_processReaderMock.Object);
        _serviceCollection.AddSingleton(_httpContextAccessor.Object);
        _serviceCollection.AddSingleton(_loggerMock.Object);

        _instanceClientMock
            .Setup(x => x.GetInstance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Guid>()))
            .ReturnsAsync(
                new Instance
                {
                    InstanceOwner = new InstanceOwner { PartyId = "1337" },
                    Process = new ProcessState
                    {
                        CurrentTask = new ProcessElementInfo { ElementId = "task1", AltinnTaskType = "signing" },
                    },
                }
            );

        _processReaderMock.Setup(s => s.GetAltinnTaskExtension(It.IsAny<string>())).Returns(_altinnTaskExtension);
    }

    [Fact]
    public async Task GetSigneesState_WhenSigneeContextIsOrg_Returns_Expected_Signees()
    {
        // Arrange
        var signedTime = DateTime.Now;

        await using var sp = _serviceCollection.BuildStrictServiceProvider();
        var controller = sp.GetRequiredService<SigningController>();

        List<SigneeContext> signeeContexts =
        [
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new OrganisationSignee
                {
                    OrgName = "org1",
                    OrgNumber = "123456789",
                    OrgParty = new Party { PartyId = 1 },
                },
                SigneeState = new SigneeContextState
                {
                    DelegationFailedReason = "delegationFailedReason",
                    IsAccessDelegated = false,
                    IsReceiptSent = false,
                    HasBeenMessagedForCallToSign = false,
                    CallToSignFailedReason = "callToSignFailedReason",
                },
                SignDocument = null,
            },
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new OrganisationSignee
                {
                    OrgName = "org1",
                    OrgNumber = "123456789",
                    OrgParty = new Party { PartyId = 1 },
                },
                SigneeState = new SigneeContextState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    HasBeenMessagedForCallToSign = false,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = signedTime,
                },
            },
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new OrganisationSignee
                {
                    OrgName = "org2",
                    OrgNumber = "987654321",
                    OrgParty = new Party { PartyId = 2 },
                },
                SigneeState = new SigneeContextState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    HasBeenMessagedForCallToSign = false,
                    CallToSignFailedReason = "callToSignFailedReason",
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = signedTime,
                },
            },
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new OrganisationSignee
                {
                    OrgName = "org2",
                    OrgNumber = "987654321",
                    OrgParty = new Party { PartyId = 2 },
                },
                SigneeState = new SigneeContextState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    HasBeenMessagedForCallToSign = true,
                    CallToSignFailedReason = null,
                },
            },
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new OrganisationSignee
                {
                    OrgName = "org2",
                    OrgNumber = "987654321",
                    OrgParty = new Party { PartyId = 2 },
                },
                SigneeState = new SigneeContextState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    HasBeenMessagedForCallToSign = true,
                    CallToSignFailedReason = null,
                },
            },
        ];

        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;

        // Assert
        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeState
                {
                    Name = null,
                    Organisation = "org1",
                    DelegationSuccessful = false,
                    NotificationStatus = NotificationStatus.Failed,
                    SignedTime = null,
                    PartyId = 1,
                },
                new SigneeState
                {
                    Name = null,
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.NotSent,
                    SignedTime = signedTime,
                    PartyId = 1,
                },
                new SigneeState
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.Failed,
                    SignedTime = signedTime,
                    PartyId = 2,
                },
                new SigneeState
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.Sent,
                    SignedTime = null,
                    PartyId = 2,
                },
                new SigneeState
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.Sent,
                    SignedTime = null,
                    PartyId = 2,
                },
            ],
        };

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(signingStateResponse));
    }

    [Fact]
    public async Task GetSigneesState_WhenSigneeContextIsPerson_Returns_Expected_Signees()
    {
        // Arrange
        await using var sp = _serviceCollection.BuildStrictServiceProvider();
        var controller = sp.GetRequiredService<SigningController>();

        List<SigneeContext> signeeContexts =
        [
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new PersonSignee
                {
                    FullName = "person1",
                    SocialSecurityNumber = "123456789",
                    Party = new Party { PartyId = 1 },
                },
                SigneeState = new SigneeContextState
                {
                    DelegationFailedReason = "delegationFailedReason",
                    IsAccessDelegated = false,
                    IsReceiptSent = false,
                    HasBeenMessagedForCallToSign = false,
                    CallToSignFailedReason = "callToSignFailedReason",
                },
                SignDocument = null,
            },
        ];
        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;

        // Assert
        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeState
                {
                    Name = "person1",
                    Organisation = null,
                    DelegationSuccessful = false,
                    NotificationStatus = NotificationStatus.Failed,
                    SignedTime = null,
                    PartyId = 1,
                },
            ],
        };

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(signingStateResponse));
    }

    [Fact]
    public async Task GetSigneesState_WhenSigneeContextIsPersonOnBehalfOfOrg_Returns_Expected_Signees()
    {
        // Arrange
        var signedTime = DateTime.Now;

        await using var sp = _serviceCollection.BuildStrictServiceProvider();
        var controller = sp.GetRequiredService<SigningController>();

        List<SigneeContext> signeeContexts =
        [
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new PersonOnBehalfOfOrgSignee
                {
                    FullName = "person1",
                    SocialSecurityNumber = "123456789",
                    Party = new Party { PartyId = 123 },
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = "org1",
                        OrgNumber = "123456789",
                        OrgParty = new Party { PartyId = 321 },
                    },
                },
                SigneeState = new SigneeContextState
                {
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsReceiptSent = false,
                    HasBeenMessagedForCallToSign = false,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = signedTime,
                },
            },
        ];
        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;

        // Assert
        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeState
                {
                    Name = "person1",
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.NotSent,
                    SignedTime = signedTime,
                    PartyId = 321,
                },
            ],
        };

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(signingStateResponse));
    }

    [Fact]
    public async Task GetSigneesState_WhenSigneeContextIsSystem_Returns_Expected_Signees()
    {
        // Arrange
        var signedTime = DateTime.Now;

        await using var sp = _serviceCollection.BuildStrictServiceProvider();
        var controller = sp.GetRequiredService<SigningController>();

        List<SigneeContext> signeeContexts =
        [
            new SigneeContext
            {
                TaskId = "task1",
                Signee = new SystemSignee
                {
                    SystemId = Guid.NewGuid(),
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = "org1",
                        OrgNumber = "123456789",
                        OrgParty = new Party { PartyId = 123 },
                    },
                },
                SigneeState = new SigneeContextState
                {
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsReceiptSent = false,
                    HasBeenMessagedForCallToSign = true,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = signedTime,
                },
            },
        ];
        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;

        // Assert
        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeState
                {
                    Name = "System",
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationStatus = NotificationStatus.Sent,
                    SignedTime = signedTime,
                    PartyId = 123,
                },
            ],
        };

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(signingStateResponse));
    }
}
