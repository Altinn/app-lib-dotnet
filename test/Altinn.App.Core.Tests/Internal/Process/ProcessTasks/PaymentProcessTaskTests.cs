using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process.ProcessTasks
{
    public class PaymentProcessTaskTests
    {
        [Fact]
        public async Task Start_calls_payment_service_start_payment()
        {
            // Arrange
            var orderDetailsFormatter = new Mock<IOrderDetailsFormatter>();
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();
            var dataService = new Mock<IDataService>();

            orderDetailsFormatter.Setup(odf => odf.GetOrderDetails(It.IsAny<Instance>())).ReturnsAsync(new OrderDetails { Currency = "NOK", OrderLines = [] });

            var processTask = new ProcessTask
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = new AltinnPaymentConfiguration
                        {
                            PaymentDataType = "payment"
                        }
                    }
                }
            };

            processReader.Setup(odf => odf.GetFlowElement(It.IsAny<string>())).Returns(processTask);

            var paymentProcessTask = new PaymentProcessTask(processReader.Object, paymentService.Object, dataService.Object, orderDetailsFormatter.Object);
            var instance = new Instance()
            {
                Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
                AppId = "ttd/test",
                Process = new ProcessState
                {
                    CurrentTask = new ProcessElementInfo
                    {
                        AltinnTaskType = "payment",
                        ElementId = "Task_1",
                    },
                },
            };

            // Act
            await paymentProcessTask.Start("Task_1", instance, null);

            // Assert
            paymentService.Verify(ps => ps.StartPayment(instance));
            paymentService.VerifyNoOtherCalls();

            dataService.Verify(dc => dc.InsertObjectAsJson(
                It.IsAny<Instance>(),
                processTask.ExtensionElements.TaskExtension.PaymentConfiguration.PaymentDataType,
                It.IsAny<object>()));
        }
    }
}
