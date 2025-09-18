using System.Net;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Api.Tests.Controllers;

public class InstancesController_AddCompleteConfirmationTests
{
    [Fact]
    public async Task AddCompleteConfirmation_SuccessfulCall_ReturnsOkResult()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();
        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();

        // Set up HttpRequest mock for SelfLinkHelper
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        requestMock.SetupGet(r => r.Path).Returns(new PathString("/ttd/test-app/instances"));

        var httpContextMock = Mock.Get(controller.HttpContext);
        httpContextMock.SetupGet(c => c.Request).Returns(requestMock.Object);

        int instanceOwnerPartyId = 1337;
        Guid instanceGuid = Guid.NewGuid();

        var expectedInstance = new Instance
        {
            Id = $"{instanceOwnerPartyId}/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerPartyId.ToString() },
            CompleteConfirmations = new List<CompleteConfirmation>
            {
                new CompleteConfirmation { StakeholderId = "test-stakeholder" },
            },
        };

        var instanceClientMock = fixture.Mock<IInstanceClient>();
        instanceClientMock
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ReturnsAsync(expectedInstance);

        var eventsClientMock = fixture.Mock<IEventsClient>();
        eventsClientMock.Setup(x => x.AddEvent(It.IsAny<string>(), It.IsAny<Instance>())).ReturnsAsync("event-id");

        // Act
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstance = Assert.IsType<Instance>(okResult.Value);
        Assert.Equal(expectedInstance.Id, returnedInstance.Id);

        instanceClientMock.Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
    }

    [Fact]
    public async Task AddCompleteConfirmation_InstanceClientThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();
        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();

        // Set up HttpRequest mock for SelfLinkHelper
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        requestMock.SetupGet(r => r.Path).Returns(new PathString("/ttd/test-app/instances"));

        var httpContextMock = Mock.Get(controller.HttpContext);
        httpContextMock.SetupGet(c => c.Request).Returns(requestMock.Object);

        int instanceOwnerPartyId = 1337;
        Guid instanceGuid = Guid.NewGuid();

        var instanceClientMock = fixture.Mock<IInstanceClient>();
        instanceClientMock
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ThrowsAsync(
                new PlatformHttpException(
                    new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Bad request") },
                    "Bad request error"
                )
            );

        // Act
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, objectResult.StatusCode);

        instanceClientMock.Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
    }

    [Fact]
    public async Task AddCompleteConfirmation_SuccessfulCall_CallsAddEventWithCorrectEventType()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();

        // Configure AppSettings to enable events registration
        var appSettings = fixture.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        appSettings.Value.RegisterEventsWithEventsComponent = true;

        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();

        // Set up HttpRequest mock for SelfLinkHelper
        var requestMock = new Mock<HttpRequest>();
        requestMock.SetupGet(r => r.Host).Returns(new HostString("localhost"));
        requestMock.SetupGet(r => r.Path).Returns(new PathString("/ttd/test-app/instances"));

        var httpContextMock = Mock.Get(controller.HttpContext);
        httpContextMock.SetupGet(c => c.Request).Returns(requestMock.Object);

        int instanceOwnerPartyId = 1337;
        Guid instanceGuid = Guid.NewGuid();

        var expectedInstance = new Instance
        {
            Id = $"{instanceOwnerPartyId}/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerPartyId.ToString() },
            CompleteConfirmations = new List<CompleteConfirmation>
            {
                new CompleteConfirmation { StakeholderId = "test-stakeholder" },
            },
        };

        var instanceClientMock = fixture.Mock<IInstanceClient>();
        instanceClientMock
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ReturnsAsync(expectedInstance);

        var eventsClientMock = fixture.Mock<IEventsClient>();
        eventsClientMock.Setup(x => x.AddEvent(It.IsAny<string>(), It.IsAny<Instance>())).ReturnsAsync("event-id");

        // Act
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);

        eventsClientMock.Verify(
            x =>
                x.AddEvent(
                    "app.instance.completeAndReadyForCleanup",
                    It.Is<Instance>(i => i.Id == expectedInstance.Id)
                ),
            Times.Once
        );

        instanceClientMock.Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
    }
}
