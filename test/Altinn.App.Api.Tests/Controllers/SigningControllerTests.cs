using System.Text.Json;
using Altinn.App.Api.Controllers;
using Altinn.App.Api.Models;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using SigneeContextSigneeState = Altinn.App.Core.Features.Signing.Models.SigneeState;
using SigneeStateDTO = Altinn.App.Api.Models.SigneeState;

namespace Altinn.App.Api.Tests.Controllers;

public class SigningControllerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IInstanceClient> _instanceClientMock;
    private readonly Mock<IAppMetadata> _appMetadataMock;
    private readonly Mock<IDataClient> _dataClientMock;
    private readonly Mock<IProcessReader> _processReaderMock;
    private readonly Mock<ILogger<SigningController>> _loggerMock;
    private readonly Mock<ISigningService> _signingServiceMock;
    private readonly SigningController _controller;
    private readonly AltinnTaskExtension _altinnTaskExtension;

    public SigningControllerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _instanceClientMock = new Mock<IInstanceClient>();
        _appMetadataMock = new Mock<IAppMetadata>();
        _dataClientMock = new Mock<IDataClient>();
        _processReaderMock = new Mock<IProcessReader>();
        _loggerMock = new Mock<ILogger<SigningController>>();
        _signingServiceMock = new Mock<ISigningService>();

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

        _serviceProviderMock.Setup(x => x.GetService(typeof(ISigningService))).Returns(_signingServiceMock.Object);

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        serviceScopeFactory.Setup(x => x.CreateScope()).Returns(serviceScope.Object);

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);

        _controller = new SigningController(
            _serviceProviderMock.Object,
            _instanceClientMock.Object,
            _appMetadataMock.Object,
            _dataClientMock.Object,
            new ModelSerializationService(new Mock<IAppModel>().Object),
            _processReaderMock.Object,
            _loggerMock.Object
        );

        _altinnTaskExtension = new()
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
        _processReaderMock.Setup(s => s.GetAltinnTaskExtension(It.IsAny<string>())).Returns(_altinnTaskExtension);
    }

    [Fact]
    public async Task GetSigneesState_WhenSigneeContextIsOrg_Returns_Expected_Signees()
    {
        // Arrange
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
                SigneeState = new SigneeContextSigneeState
                {
                    DelegationFailedReason = "delegationFailedReason",
                    IsAccessDelegated = false,
                    IsReceiptSent = false,
                    IsMessagedForCallToSign = false,
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
                SigneeState = new SigneeContextSigneeState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsMessagedForCallToSign = false,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = DateTime.Now,
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
                SigneeState = new SigneeContextSigneeState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsMessagedForCallToSign = false,
                    CallToSignFailedReason = "callToSignFailedReason",
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = DateTime.Now,
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
                SigneeState = new SigneeContextSigneeState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsMessagedForCallToSign = true,
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
                SigneeState = new SigneeContextSigneeState
                {
                    IsReceiptSent = false,
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsMessagedForCallToSign = true,
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
        var actionResult = await _controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;
        // Assert

        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeStateDTO
                {
                    Name = null,
                    Organisation = "org1",
                    DelegationSuccessful = false,
                    NotificationSuccessful = NotificationState.Failed,
                    HasSigned = false,
                    PartyId = 1,
                },
                new SigneeStateDTO
                {
                    Name = null,
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.NotSent,
                    HasSigned = true,
                    PartyId = 1,
                },
                new SigneeStateDTO
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.Failed,
                    HasSigned = true,
                    PartyId = 2,
                },
                new SigneeStateDTO
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.Sent,
                    HasSigned = false,
                    PartyId = 2,
                },
                new SigneeStateDTO
                {
                    Name = null,
                    Organisation = "org2",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.Sent,
                    HasSigned = false,
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
                SigneeState = new SigneeContextSigneeState
                {
                    DelegationFailedReason = "delegationFailedReason",
                    IsAccessDelegated = false,
                    IsReceiptSent = false,
                    IsMessagedForCallToSign = false,
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
        var actionResult = await _controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;
        // Assert

        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeStateDTO
                {
                    Name = "person1",
                    Organisation = null,
                    DelegationSuccessful = false,
                    NotificationSuccessful = NotificationState.Failed,
                    HasSigned = false,
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
                SigneeState = new SigneeContextSigneeState
                {
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsReceiptSent = false,
                    IsMessagedForCallToSign = false,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = DateTime.Now,
                },
            },
        ];
        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await _controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;
        // Assert

        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeStateDTO
                {
                    Name = "person1",
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.NotSent,
                    HasSigned = true,
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
                SigneeState = new SigneeContextSigneeState
                {
                    DelegationFailedReason = null,
                    IsAccessDelegated = true,
                    IsReceiptSent = false,
                    IsMessagedForCallToSign = true,
                    CallToSignFailedReason = null,
                },
                SignDocument = new SignDocument
                {
                    DataElementSignatures = [],
                    Id = "signDocument",
                    InstanceGuid = "instanceGuid",
                    SignedTime = DateTime.Now,
                },
            },
        ];
        _signingServiceMock
            .Setup(s =>
                s.GetSigneeContexts(It.IsAny<InstanceDataUnitOfWork>(), _altinnTaskExtension.SignatureConfiguration!)
            )
            .ReturnsAsync(signeeContexts);

        // Act
        var actionResult = await _controller.GetSigneesState("tdd", "app", 1337, Guid.NewGuid());

        var okResult = actionResult as OkObjectResult;
        Assert.NotNull(okResult);

        var signingStateResponse = okResult.Value as SigningStateResponse;
        // Assert

        var expected = new SigningStateResponse
        {
            SigneeStates =
            [
                new SigneeStateDTO
                {
                    Name = "System",
                    Organisation = "org1",
                    DelegationSuccessful = true,
                    NotificationSuccessful = NotificationState.Sent,
                    HasSigned = true,
                    PartyId = 123,
                },
            ],
        };

        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(signingStateResponse));
    }
}
