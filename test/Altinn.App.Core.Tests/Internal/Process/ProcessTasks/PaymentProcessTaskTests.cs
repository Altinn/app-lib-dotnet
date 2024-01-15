using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process.ProcessTasks
{
    public class PaymentProcessTaskTests
    {
        [Fact]
        public async Task Start_calls_order_details_service()
        {
            // Arrange
            var orderDetailsFormatter = new Mock<IOrderDetailsFormatter>();
            orderDetailsFormatter.Setup(odf => odf.GetOrderDetails(It.IsAny<Instance>())).ReturnsAsync(new PaymentOrder { Currency = "NOK", OrderLines = [] });

            var paymentProcessTask = new PaymentProcessTask(orderDetailsFormatter.Object);
            var instance = new Instance()
            {
                Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
                AppId = "ttd/test",
            };

            // Act
            await paymentProcessTask.Start("Task_1", instance, null);

            // Assert
            orderDetailsFormatter.Verify(odf => odf.GetOrderDetails(instance));
            orderDetailsFormatter.VerifyNoOtherCalls();
        }
    }
}
