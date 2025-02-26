using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing.Delegation;

public class SigningDelegationServiceTests
{
    private readonly SigneeContextSignee _signee = new SigneeContextSignee.PersonSignee
    {
        FullName = "Testperson 1",
        SocialSecurityNumber = "123456678233",
        Party = new Party(),
    };

    [Fact]
    public async Task RevokeSigneeRights_RevokeSigneeRights()
    {
        // Arrange
        var accessManagementClient = new Mock<IAccessManagementClient>();
        var logger = new Mock<ILogger<SigningDelegationService>>();
        var service = new SigningDelegationService(accessManagementClient.Object, logger.Object);
        var taskId = "taskId";
        Guid instanceGuid = Guid.NewGuid();
        var instanceId = "instanceOwnerPartyId" + "/" + instanceGuid;
        Guid InstanceOwnerPartyUuid = Guid.NewGuid();
        var appIdentifier = new AppIdentifier("testOrg", "testApp");
        var signeeContexts = new List<SigneeContext>()
        {
            new()
            {
                TaskId = taskId,

                SigneeState = new SigneeState() { IsAccessDelegated = true },
                Signee = _signee,
            },
        };
        var ct = new CancellationToken();

        // Act
        (signeeContexts, var success) = await service.RevokeSigneeRights(
            taskId,
            instanceId,
            InstanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct
        );

        // Assert
        Assert.True(success);
        Assert.False(signeeContexts[0].SigneeState.IsAccessDelegated);
    }

    [Fact]
    public async Task RevokeSigneeRights_SigneeStateIsNotDelegated()
    {
        // Arrange
        var accessManagementClient = new Mock<IAccessManagementClient>();
        accessManagementClient
            .Setup(x => x.RevokeRights(It.IsAny<DelegationRequest>(), It.IsAny<CancellationToken>()))
            .Verifiable();
        var logger = new Mock<ILogger<SigningDelegationService>>();
        var service = new SigningDelegationService(accessManagementClient.Object, logger.Object);
        var taskId = "taskId";
        Guid instanceGuid = Guid.NewGuid();
        var instanceId = "instanceOwnerPartyId" + "/" + instanceGuid;
        Guid InstanceOwnerPartyUuid = Guid.NewGuid();
        var appIdentifier = new AppIdentifier("testOrg", "testApp");
        var signeeContexts = new List<SigneeContext>()
        {
            new()
            {
                TaskId = taskId,
                SigneeState = new SigneeState() { IsAccessDelegated = false }, // Signee is not delegated signing rights
                Signee = _signee,
            },
        };
        var ct = new CancellationToken();

        // Act
        (signeeContexts, var success) = await service.RevokeSigneeRights(
            taskId,
            instanceId,
            InstanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct
        );

        // Assert
        Assert.True(success);
        Assert.False(signeeContexts[0].SigneeState.IsAccessDelegated);
        accessManagementClient.Verify(
            x => x.RevokeRights(It.IsAny<DelegationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never // No rights should be revoked, as the signee is not delegated signing rights in the first place
        );
    }

    [Fact]
    public async Task DelegateSigneeRights_DelegateSigneeRights()
    {
        // Arrange
        var accessManagementClient = new Mock<IAccessManagementClient>();
        var logger = new Mock<ILogger<SigningDelegationService>>();
        var service = new SigningDelegationService(accessManagementClient.Object, logger.Object);
        var taskId = "taskId";
        Guid instanceGuid = Guid.NewGuid();
        var instanceId = "instanceOwnerPartyId" + "/" + instanceGuid;
        Guid InstanceOwnerPartyUuid = Guid.NewGuid();
        var appIdentifier = new AppIdentifier("testOrg", "testApp");
        var signeeContexts = new List<SigneeContext>()
        {
            new()
            {
                TaskId = taskId,
                SigneeState = new SigneeState() { IsAccessDelegated = false },
                Signee = _signee,
            },
        };
        var ct = new CancellationToken();

        // Act
        (signeeContexts, var success) = await service.DelegateSigneeRights(
            taskId,
            instanceId,
            InstanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct
        );

        // Assert
        Assert.True(success);
        Assert.True(signeeContexts[0].SigneeState.IsAccessDelegated);
    }

    [Fact]
    public async Task DelegateSigneeRights_WhenAlreadyDelegated_DoesNotInvokeClient()
    {
        // Arrange
        var accessManagementClient = new Mock<IAccessManagementClient>();
        accessManagementClient
            .Setup(x => x.DelegateRights(It.IsAny<DelegationRequest>(), It.IsAny<CancellationToken>()))
            .Verifiable();
        var logger = new Mock<ILogger<SigningDelegationService>>();
        var service = new SigningDelegationService(accessManagementClient.Object, logger.Object);
        var taskId = "taskId";
        Guid instanceGuid = Guid.NewGuid();
        var instanceId = "instanceOwnerPartyId" + "/" + instanceGuid;
        Guid InstanceOwnerPartyUuid = Guid.NewGuid();
        var appIdentifier = new AppIdentifier("testOrg", "testApp");
        var signeeContexts = new List<SigneeContext>()
        {
            new()
            {
                TaskId = taskId,
                SigneeState = new SigneeState() { IsAccessDelegated = true }, // Signee is already delegated signing rights
                Signee = _signee,
            },
        };
        var ct = new CancellationToken();

        // Act
        (signeeContexts, var success) = await service.DelegateSigneeRights(
            taskId,
            instanceId,
            InstanceOwnerPartyUuid,
            appIdentifier,
            signeeContexts,
            ct
        );

        // Assert
        Assert.True(success);
        Assert.True(signeeContexts[0].SigneeState.IsAccessDelegated);
        accessManagementClient.Verify(
            x => x.DelegateRights(It.IsAny<DelegationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never // No rights should be delegated, as the signee already has signing rights
        );
    }
}
