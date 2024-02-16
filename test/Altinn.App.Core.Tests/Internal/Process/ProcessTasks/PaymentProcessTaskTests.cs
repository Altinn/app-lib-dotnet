using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Models;
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
            var orderDetailsFormatter = new Mock<IOrderDetailsCalculator>();
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();
            var dataService = new Mock<IDataService>();
            
            AltinnPaymentConfiguration paymentConfiguration = new()
            {
                PaymentDataType = "paymentInformation"
            };

            orderDetailsFormatter.Setup(odf => odf.CalculateOrderDetails(It.IsAny<Instance>())).ReturnsAsync(new OrderDetails { Currency = "NOK", OrderLines = [] });

            var processTask = new ProcessTask
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = paymentConfiguration
                    }
                }
            };

            processReader.Setup(odf => odf.GetAltinnTaskExtension(It.IsAny<string>())).Returns(processTask.ExtensionElements.TaskExtension);

            var paymentProcessTask = new PaymentProcessTask(processReader.Object, paymentService.Object, orderDetailsFormatter.Object);
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
            paymentService.Verify(ps => ps.StartPayment(instance, paymentConfiguration));
            paymentService.VerifyNoOtherCalls();
        }
    }
}
