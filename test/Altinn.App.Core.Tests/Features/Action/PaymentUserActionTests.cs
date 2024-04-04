#nullable disable
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
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
using Json.Patch;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Tests.Features.Action;

public class PaymentUserActionTests
{
    private readonly Mock<IDataService> _dataServiceMock = new();
    private readonly Mock<IPaymentService> _paymentServiceMock = new();

    [Fact]
    public async void HandleAction_returns_ok()
    {
        // Arrange
        var instance = new Instance()
        {
            Id = "500000/b194e9f5-02d0-41bc-8461-a0cbac8a6efc",
            InstanceOwner = new InstanceOwner
            {
                PartyId = "5000",
            },
            Process = new ProcessState
            {
                CurrentTask = new ProcessElementInfo
                {
                    ElementId = "Task2"
                }
            },
            Data =
            [
                new DataElement
                {
                    Id = "a499c3ef-e88a-436b-8650-1c43e5037ada",
                    DataType = "Model"
                }
            ]
        };

        AltinnPaymentConfiguration paymentConfiguration = new()
        {
            PaymentProcessorId = "paymentProcessorId",
            PaymentDataType = "paymentInformation"
        };

        PaymentInformation paymentInformation = new()
        {
            TaskId = instance.Process.CurrentTask.ElementId,
            PaymentProcessorId = paymentConfiguration.PaymentProcessorId,
            OrderDetails = new OrderDetails
            {
                Currency = "NOK",
                OrderLines = []
            },
            PaymentDetails = new PaymentDetails
            {
                PaymentId = "1",
                Status = PaymentStatus.Created,
                RedirectUrl = "https://example.com",
            }
        };

        var userActionContext = new UserActionContext(instance, 1337);

        _paymentServiceMock.Setup(x => x.StartPayment(It.IsAny<Instance>(), It.IsAny<AltinnPaymentConfiguration>())).ReturnsAsync((paymentInformation, false));

        // Act
        PaymentUserAction userAction = CreatePaymentUserAction();
        UserActionResult result = await userAction.HandleAction(userActionContext);

        // Assert
        result.Should().BeEquivalentTo(UserActionResult.RedirectResult(new Uri(paymentInformation.PaymentDetails.RedirectUrl)));
    }

    private PaymentUserAction CreatePaymentUserAction(string testBpmnFilename = "payment-task-process.bpmn")
    {
        IProcessReader processReader = ProcessTestUtils.SetupProcessReader(testBpmnFilename, Path.Combine("Features", "Action", "TestData"));
        return new PaymentUserAction(processReader, _paymentServiceMock.Object, NullLogger<PaymentUserAction>.Instance);
    }
}