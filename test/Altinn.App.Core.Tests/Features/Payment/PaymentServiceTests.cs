#nullable enable

using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Payment;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;
    private readonly Mock<IPaymentProcessor> _paymentProcessor = new(MockBehavior.Strict);
    private readonly Mock<IOrderDetailsCalculator> _orderDetailsCalculator = new(MockBehavior.Strict);
    private readonly Mock<IDataService> _dataService = new(MockBehavior.Strict);
    private readonly Mock<ILogger<PaymentService>> _logger = new();

    public PaymentServiceTests()
    {
        _paymentService =
            new PaymentService([_paymentProcessor.Object], _orderDetailsCalculator.Object, _dataService.Object, _logger.Object);
    }

    [Fact]
    public async Task StartPayment_ReturnsRedirectUrl_WhenPaymentStartedSuccessfully()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));
        _dataService.Setup(ds =>
                ds.InsertJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new DataElement());
        _paymentProcessor.Setup(pp => pp.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(paymentInformation.PaymentDetails);

        // Act
        (PaymentInformation paymentInformationResult, bool alreadyPaid) = await _paymentService.StartPayment(instance, paymentConfiguration);

        // Assert
        paymentInformationResult.PaymentDetails.Should().NotBeNull();
        paymentInformationResult.PaymentDetails!.RedirectUrl.Should().Be(paymentInformation.PaymentDetails!.RedirectUrl);
        paymentInformationResult.OrderDetails.Should().BeEquivalentTo(order);
        paymentInformationResult.PaymentProcessorId.Should().Be(paymentInformation.PaymentProcessorId);
        alreadyPaid.Should().BeFalse();

        // Verify calls
        _orderDetailsCalculator.Verify(odc => odc.CalculateOrderDetails(instance), Times.Once);
        _paymentProcessor.Verify(pp => pp.StartPayment(instance, order), Times.Once);
        _dataService.Verify(ds => ds.InsertJsonObject(
            It.IsAny<InstanceIdentifier>(),
            paymentConfiguration.PaymentDataType!,
            It.IsAny<object>()));
    }

    [Fact]
    public async Task StartPayment_ReturnsAlreadyPaidTrue_WhenPaymentIsAlreadyPaid()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();

        _paymentProcessor.Setup(pp => pp.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(),
                new PaymentInformation
                {
                    TaskId = "Task_1", PaymentProcessorId = "PaymentProcessorId",
                    PaymentDetails = new PaymentDetails { Status = PaymentStatus.Paid, PaymentId = "id", RedirectUrl = "url" },
                    OrderDetails = order
                }));

        // Act
        (PaymentInformation paymentInformationResult, bool alreadyPaid) = await _paymentService.StartPayment(instance, paymentConfiguration);

        // Assert
        paymentInformationResult.PaymentDetails.Should().NotBeNull();
        alreadyPaid.Should().BeTrue();
    }

    [Fact]
    public async Task StartPayment_ThrowsException_WhenOrderDetailsCannotBeRetrieved()
    {
        Instance instance = CreateInstance();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));

        _orderDetailsCalculator.Setup(odc => odc.CalculateOrderDetails(instance)).ThrowsAsync(new Exception());

        _paymentProcessor.Setup(x => x.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _paymentService.StartPayment(instance, paymentConfiguration));
    }

    [Fact]
    public async Task StartPayment_ThrowsException_WhenPaymentCannotBeStarted()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));
        _orderDetailsCalculator.Setup(pp => pp.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _paymentProcessor.Setup(x => x.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        _paymentProcessor.Setup(pp => pp.StartPayment(instance, order)).ThrowsAsync(new Exception());

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _paymentService.StartPayment(instance, paymentConfiguration));
    }

    [Fact]
    public async Task StartPayment_ThrowsException_WhenPaymentInformationCannotBeStored()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));
        _dataService.Setup(ds =>
                ds.InsertJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception());
        _paymentProcessor.Setup(pp => pp.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        _paymentProcessor.Setup(pp => pp.StartPayment(instance, order)).ReturnsAsync(paymentInformation.PaymentDetails);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _paymentService.StartPayment(instance, paymentConfiguration));
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ReturnsNull_WhenNoPaymentInformationFound()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));

        // Act
        PaymentInformation? result =
            await _paymentService.CheckAndStorePaymentStatus(instance, paymentConfiguration);

        // Assert
        result.Should().NotBeNull();
        instance.Process.CurrentTask.ElementId.Should().Be(result!.TaskId);
        paymentConfiguration.PaymentProcessorId.Should().Be(result!.PaymentProcessorId);
        order.Should().BeEquivalentTo(result!.OrderDetails);
        result.PaymentDetails.Should().BeNull();
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ThrowsException_WhenUnableToCheckPaymentStatus()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        _orderDetailsCalculator.Setup(odc => odc.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _paymentProcessor.Setup(x => x.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        
        _paymentProcessor.Setup(pp =>
                pp.GetPaymentStatus(It.IsAny<Instance>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync((PaymentStatus?)null);

        var paymentService =
            new PaymentService([_paymentProcessor.Object], _orderDetailsCalculator.Object, _dataService.Object, _logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() =>
            paymentService.CheckAndStorePaymentStatus(instance, paymentConfiguration));
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ReturnsPaymentInformation_WhenPaymentStatusCheckedSuccessfully()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _dataService.Setup(ds =>
            ds.UpdateJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>())).ReturnsAsync(new DataElement());

        _paymentProcessor.Setup(x => x.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        
        _paymentProcessor.Setup(pp =>
                pp.GetPaymentStatus(It.IsAny<Instance>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(PaymentStatus.Paid);

        var paymentService =
            new PaymentService([_paymentProcessor.Object], _orderDetailsCalculator.Object, _dataService.Object, _logger.Object);

        // Act
        PaymentInformation? result =
            await paymentService.CheckAndStorePaymentStatus(instance, paymentConfiguration);

        // Assert
        result.Should().NotBeNull();
        result!.PaymentDetails.Should().NotBeNull();
        result.PaymentDetails!.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task CancelPayment_ShouldCallCancelAndDelete_WhenPaymentIsNotPaid()
    {
        // Arrange
        Instance instance = CreateInstance();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        OrderDetails orderDetails = CreateOrderDetails();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        paymentInformation.PaymentDetails!.Status = PaymentStatus.Cancelled;
        
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _dataService.Setup(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _dataService.Setup(x => x.InsertJsonObject(It.IsAny<InstanceIdentifier>(), paymentConfiguration.PaymentDataType!, It.IsAny<object>()))
            .ReturnsAsync(new DataElement());

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(orderDetails);

        _paymentProcessor.Setup(x => x.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);

        _paymentProcessor.Setup(pp => pp.TerminatePayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()))
            .ReturnsAsync(true);

        _paymentProcessor.Setup(x => x.StartPayment(instance, orderDetails)).ReturnsAsync(paymentInformation.PaymentDetails);

        // Act
        await _paymentService.StartPayment(instance, paymentConfiguration);

        // Assert
        _paymentProcessor.Verify(pp => pp.TerminatePayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()), Times.Once);
        _dataService.Verify(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CancelPayment_ShouldNotDelete_WhenPaymentCancellationFails()
    {
        // Arrange
        Instance instance = CreateInstance();
        OrderDetails orderDetails = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation(paymentConfiguration.PaymentProcessorId!);

        _orderDetailsCalculator.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(orderDetails);
        _dataService.Setup(ds =>
                ds.InsertJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new DataElement());
        _paymentProcessor.Setup(pp => pp.PaymentProcessorId).Returns(paymentConfiguration.PaymentProcessorId!);
        _paymentProcessor.Setup(p => p.StartPayment(instance, orderDetails)).ReturnsAsync(paymentInformation.PaymentDetails);
        
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _paymentProcessor.Setup(pp => pp.TerminatePayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<PaymentException>(async () => await _paymentService.StartPayment(instance, paymentConfiguration));

        // Act & Assert
        _paymentProcessor.Verify(pp => pp.TerminatePayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()), Times.Once);
        _dataService.Verify(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()), Times.Never);
    }

    private static PaymentInformation CreatePaymentInformation(string paymentProcssorId)
    {
        return new PaymentInformation
        {
            TaskId = "taskId",
            PaymentProcessorId = paymentProcssorId,
            OrderDetails = new OrderDetails
            {
                Currency = "NOK",
                OrderLines = []
            },
            PaymentDetails = new PaymentDetails
            {
                RedirectUrl = "Redirect URL",
                PaymentId = "PaymentReference",
                Status = PaymentStatus.Created
            }
        };
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

    private static OrderDetails CreateOrderDetails()
    {
        return new OrderDetails()
        {
            Currency = "NOK",
            OrderLines =
            [
                new PaymentOrderLine
                {
                    Id = "001",
                    Name = "Fee",
                    PriceExVat = 1000,
                    VatPercent = 25,
                }
            ]
        };
    }

    private static AltinnPaymentConfiguration CreatePaymentConfiguration()
    {
        return new AltinnPaymentConfiguration { PaymentProcessorId = "paymentProcessorId", PaymentDataType = "paymentInformation" };
    }
}