using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
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
            var orderDetailsCalculator = new Mock<IOrderDetailsCalculator>();
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();

            AltinnPaymentConfiguration paymentConfiguration = new()
            {
                PaymentDataType = "paymentInformation"
            };

            orderDetailsCalculator.Setup(odf => odf.CalculateOrderDetails(It.IsAny<Instance>()))
                .ReturnsAsync(new OrderDetails { Currency = "NOK", OrderLines = [] });

            ProcessTask processTask = new()
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = paymentConfiguration
                    }
                }
            };

            processReader.Setup(odf => odf.GetAltinnTaskExtension(It.IsAny<string>()))
                .Returns(processTask.ExtensionElements.TaskExtension);

            PaymentProcessTask paymentProcessTask = new(processReader.Object, paymentService.Object, orderDetailsCalculator.Object);
            Instance instance = CreateInstance();

            // Act
            await paymentProcessTask.Start("Task_1", instance);

            // Assert
            paymentService.Verify(ps => ps.StartPayment(instance, paymentConfiguration));
            paymentService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Start_WhenPaymentConfigurationIsNull_ThrowsException()
        {
            // Arrange
            var orderDetailsFormatter = new Mock<IOrderDetailsCalculator>();
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();

            ProcessTask processTask = new()
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = null
                    }
                }
            };

            processReader.Setup(odf => odf.GetAltinnTaskExtension(It.IsAny<string>()))
                .Returns(processTask.ExtensionElements.TaskExtension);

            PaymentProcessTask paymentProcessTask = new(processReader.Object, paymentService.Object, orderDetailsFormatter.Object);
            Instance instance = CreateInstance();

            // Act & Assert
            await Assert.ThrowsAsync<PaymentException>(() => paymentProcessTask.Start("Task_1", instance));
        }

        [Fact]
        public async Task Start_WhenNoOrderDetailsCalculator_ThrowsException()
        {
            // Arrange
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();

            ProcessTask processTask = new()
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = null
                    }
                }
            };

            processReader.Setup(odf => odf.GetAltinnTaskExtension(It.IsAny<string>()))
                .Returns(processTask.ExtensionElements.TaskExtension);

            PaymentProcessTask paymentProcessTask = new(processReader.Object, paymentService.Object, null);
            Instance instance = CreateInstance();

            // Act & Assert
            await Assert.ThrowsAsync<PaymentException>(() => paymentProcessTask.Start("Task_1", instance));
        }

        [Fact]
        public async Task Start_WhenInstanceIsNull_ThrowsException()
        {
            // Arrange
            var orderDetailsFormatter = new Mock<IOrderDetailsCalculator>();
            var processReader = new Mock<IProcessReader>();
            var paymentService = new Mock<IPaymentService>();

            AltinnPaymentConfiguration paymentConfiguration = new()
            {
                PaymentDataType = "paymentInformation"
            };

            ProcessTask processTask = new()
            {
                ExtensionElements = new ExtensionElements
                {
                    TaskExtension = new AltinnTaskExtension
                    {
                        PaymentConfiguration = paymentConfiguration
                    }
                }
            };

            processReader.Setup(odf => odf.GetAltinnTaskExtension(It.IsAny<string>()))
                .Returns(processTask.ExtensionElements.TaskExtension);

            PaymentProcessTask paymentProcessTask = new(processReader.Object, paymentService.Object, orderDetailsFormatter.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => paymentProcessTask.Start("Task_1", null!));
        }

        private static Instance CreateInstance()
        {
            return new Instance()
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
        }
    }
}