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
using FluentAssertions;

namespace Altinn.App.Api.Tests.Controllers;

public class InstancesController_AddCompleteConfirmationTests
{
    [Fact]
    public async Task SuccessfulCall_ReturnsOkResult()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();

        const int instanceOwnerPartyId = 1337;
        var instanceGuid = Guid.NewGuid();

        var expectedInstance = new Instance
        {
            Id = $"{instanceOwnerPartyId}/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerPartyId.ToString() },
            CompleteConfirmations = new List<CompleteConfirmation>
            {
                new CompleteConfirmation { StakeholderId = "test-stakeholder" },
            },
        };

        fixture
            .Mock<IInstanceClient>()
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ReturnsAsync(expectedInstance);

        fixture
            .Mock<IEventsClient>()
            .Setup(x => x.AddEvent(It.IsAny<string>(), It.IsAny<Instance>()))
            .ReturnsAsync("event-id");

        fixture.Mock<HttpContext>().Setup(hc => hc.Request).Returns(Mock.Of<HttpRequest>());

        // Act
        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var resultValue = result.Result.Should().BeOfType<OkObjectResult>().Which.Value;
        resultValue.Should().NotBeNull();
        var returnedInstance = resultValue.Should().BeOfType<Instance>().Which;
        returnedInstance.Id.Should().Be(expectedInstance.Id);

        fixture
            .Mock<IInstanceClient>()
            .Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
        fixture.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InstanceClientThrowsException_ReturnsErrorResponse()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();

        const int instanceOwnerPartyId = 1337;
        var instanceGuid = Guid.NewGuid();

        fixture
            .Mock<IInstanceClient>()
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ThrowsAsync(
                new PlatformHttpException(
                    new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Bad request") },
                    "Bad request error"
                )
            );

        fixture.Mock<HttpContext>().Setup(hc => hc.Request).Returns(Mock.Of<HttpRequest>());

        // Act
        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var resultValue = result.Result.Should().BeOfType<ObjectResult>().Which;
        resultValue.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        fixture
            .Mock<IInstanceClient>()
            .Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
        fixture.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulCall_CallsAddEventWithCorrectEventType()
    {
        // Arrange
        using var fixture = InstancesControllerFixture.Create();

        // Configure AppSettings to enable events registration
        var appSettings = fixture.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        appSettings.Value.RegisterEventsWithEventsComponent = true;

        const int instanceOwnerPartyId = 1337;
        var instanceGuid = Guid.NewGuid();

        var expectedInstance = new Instance
        {
            Id = $"{instanceOwnerPartyId}/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerPartyId.ToString() },
            CompleteConfirmations = new List<CompleteConfirmation>
            {
                new CompleteConfirmation { StakeholderId = "test-stakeholder" },
            },
        };

        fixture
            .Mock<IInstanceClient>()
            .Setup(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid))
            .ReturnsAsync(expectedInstance);

        fixture
            .Mock<IEventsClient>()
            .Setup(x => x.AddEvent(It.IsAny<string>(), It.IsAny<Instance>()))
            .ReturnsAsync("event-id");

        fixture.Mock<HttpContext>().Setup(hc => hc.Request).Returns(Mock.Of<HttpRequest>());

        // Act
        var controller = fixture.ServiceProvider.GetRequiredService<InstancesController>();
        var result = await controller.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);

        // Assert
        var resultValue = result.Result.Should().BeOfType<OkObjectResult>().Which.Value;
        resultValue.Should().NotBeNull();

        fixture
            .Mock<IEventsClient>()
            .Verify(
                x =>
                    x.AddEvent(
                        "app.instance.completeAndReadyForCleanup",
                        It.Is<Instance>(i => i.Id == expectedInstance.Id)
                    ),
                Times.Once
            );

        fixture
            .Mock<IInstanceClient>()
            .Verify(x => x.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid), Times.Once);
        fixture.VerifyNoOtherCalls();
    }
}
