using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process.ProcessTasks;

public class PaymentProcessTaskTests
{
    public class ProcessTaskDataLockerTests
    {
        private readonly Mock<IPdfService> _pdfServiceMock;
        private readonly Mock<IDataClient> _dataClientMock;
        private readonly Mock<IProcessReader> _processReaderMock;
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly PaymentProcessTask _paymentProcessTask;

        public ProcessTaskDataLockerTests()
        {
            _pdfServiceMock = new Mock<IPdfService>();
            _dataClientMock = new Mock<IDataClient>();
            _processReaderMock = new Mock<IProcessReader>();
            _paymentServiceMock = new Mock<IPaymentService>();

            _paymentProcessTask = new PaymentProcessTask(
                _pdfServiceMock.Object,
                _dataClientMock.Object,
                _processReaderMock.Object,
                _paymentServiceMock.Object
            );
        }

        [Fact]
        public async Task Start_ShouldReturnCompletedTask()
        {
            // Arrange
            Instance instance = CreateInstance();
            string taskId = instance.Process.CurrentTask.ElementId;

            // Act
            await _paymentProcessTask.Start(taskId, instance);
        }

        [Fact]
        public async Task End_PaymentCompleted_ShouldGeneratePdfReceipt()
        {
            Instance instance = CreateInstance();
            string taskId = instance.Process.CurrentTask.ElementId;

            var altinnTaskExtension = new AltinnTaskExtension
            {
                PaymentConfiguration = new AltinnPaymentConfiguration { PaymentDataType = "paymentDataType" }
            };

            _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);

            _paymentServiceMock
                .Setup(x => x.IsPaymentCompleted(It.IsAny<Instance>(), It.IsAny<AltinnPaymentConfiguration>()))
                .ReturnsAsync(true);

            // Act
            await _paymentProcessTask.End(taskId, instance);

            // Assert
            _pdfServiceMock.Verify(x => x.GeneratePdf(instance, taskId, CancellationToken.None));
            _dataClientMock.Verify(x =>
                x.InsertBinaryData(
                    instance.Id,
                    altinnTaskExtension.PaymentConfiguration.PaymentDataType,
                    "application/pdf",
                    "Betalingskvittering.pdf",
                    It.IsAny<Stream>(),
                    taskId
                )
            );
        }

        [Fact]
        public async Task End_PaymentNotCompleted_ShouldThrowException()
        {
            Instance instance = CreateInstance();
            string taskId = instance.Process.CurrentTask.ElementId;

            var altinnTaskExtension = new AltinnTaskExtension
            {
                PaymentConfiguration = new AltinnPaymentConfiguration { PaymentDataType = "paymentDataType" }
            };

            _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);

            _paymentServiceMock
                .Setup(x => x.IsPaymentCompleted(It.IsAny<Instance>(), It.IsAny<AltinnPaymentConfiguration>()))
                .ReturnsAsync(false);

            // Act and assert
            _pdfServiceMock.Verify(x => x.GeneratePdf(instance, taskId, CancellationToken.None), Times.Never);
            _dataClientMock.Verify(
                x =>
                    x.InsertBinaryData(
                        instance.Id,
                        altinnTaskExtension.PaymentConfiguration.PaymentDataType,
                        "application/pdf",
                        "Betalingskvittering.pdf",
                        It.IsAny<Stream>(),
                        taskId
                    ),
                Times.Never
            );

            await Assert.ThrowsAsync<PaymentException>(async () => await _paymentProcessTask.End(taskId, instance));
        }

        [Fact]
        public async Task Abandon_ShouldCancelAndDelete()
        {
            Instance instance = CreateInstance();
            string taskId = instance.Process.CurrentTask.ElementId;

            var altinnTaskExtension = new AltinnTaskExtension
            {
                PaymentConfiguration = new AltinnPaymentConfiguration { PaymentDataType = "paymentDataType" }
            };

            _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);

            // Act
            await _paymentProcessTask.Abandon(taskId, instance);

            // Assert
            _paymentServiceMock.Verify(x => x.CancelAndDelete(instance, altinnTaskExtension.PaymentConfiguration));
        }

        private static Instance CreateInstance()
        {
            return new Instance()
            {
                Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
                AppId = "ttd/test",
                Process = new ProcessState
                {
                    CurrentTask = new ProcessElementInfo { AltinnTaskType = "payment", ElementId = "Task_1", },
                },
            };
        }
    }
}
