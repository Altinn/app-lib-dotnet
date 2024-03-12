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
    private readonly Mock<IOrderDetailsCalculator> _orderDetailsFormatter = new(MockBehavior.Strict);
    private readonly Mock<IDataService> _dataService = new(MockBehavior.Strict);
    private readonly Mock<ILogger<PaymentService>> _logger = new();

    public PaymentServiceTests()
    {
        _paymentService =
            new PaymentService(_paymentProcessor.Object, _orderDetailsFormatter.Object, _dataService.Object, _logger.Object);
    }

    [Fact]
    public async Task StartPayment_ReturnsRedirectUrl_WhenPaymentStartedSuccessfully()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation();

        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));
        _dataService.Setup(ds =>
                ds.InsertJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new DataElement());
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(paymentInformation);

        // Act
        PaymentInformation paymentInformationResult =
            await _paymentService.StartPayment(instance, paymentConfiguration);

        // Assert
        paymentInformationResult.RedirectUrl.Should().Be(paymentInformation.RedirectUrl);
        paymentInformationResult.PaymentProcessorId.Should().Be(paymentInformation.PaymentProcessorId);

        // Verify calls
        _orderDetailsFormatter.Verify(p => p.CalculateOrderDetails(instance), Times.Once);
        _paymentProcessor.Verify(p => p.StartPayment(instance, order), Times.Once);
        _dataService.Verify(dc => dc.InsertJsonObject(
            It.IsAny<InstanceIdentifier>(),
            paymentConfiguration.PaymentDataType!,
            It.IsAny<object>()));
    }

    [Fact]
    public async Task StartPayment_ThrowsException_WhenOrderDetailsCannotBeRetrieved()
    {
        Instance instance = CreateInstance();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));

        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ThrowsAsync(new Exception());

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
        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ThrowsAsync(new Exception());

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
        PaymentInformation paymentInformation = CreatePaymentInformation();

        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);
        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));
        _dataService.Setup(ds =>
                ds.InsertJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception());
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(paymentInformation);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _paymentService.StartPayment(instance, paymentConfiguration));
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ReturnsNull_WhenNoPaymentInformationFound()
    {
        Instance instance = CreateInstance();

        var paymentConfiguration = new AltinnPaymentConfiguration { PaymentDataType = "paymentInformation" };

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.Empty, null));

        // Act
        PaymentInformation? result =
            await _paymentService.CheckAndStorePaymentInformation(instance, paymentConfiguration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ThrowsException_WhenUnableToCheckPaymentStatus()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation();

        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));
        _paymentProcessor.Setup(pp =>
                pp.GetPaymentStatus(It.IsAny<Instance>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync((PaymentStatus?)null);

        var paymentService =
            new PaymentService(_paymentProcessor.Object, _orderDetailsFormatter.Object, _dataService.Object, _logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() =>
            paymentService.CheckAndStorePaymentInformation(instance, paymentConfiguration));
    }

    [Fact]
    public async Task CheckAndStorePaymentInformation_ReturnsPaymentInformation_WhenPaymentStatusCheckedSuccessfully()
    {
        Instance instance = CreateInstance();
        OrderDetails order = CreateOrderDetails();
        AltinnPaymentConfiguration paymentConfiguration = CreatePaymentConfiguration();
        PaymentInformation paymentInformation = CreatePaymentInformation();

        _orderDetailsFormatter.Setup(p => p.CalculateOrderDetails(instance)).ReturnsAsync(order);

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _dataService.Setup(ds =>
            ds.UpdateJsonObject(It.IsAny<InstanceIdentifier>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>())).ReturnsAsync(new DataElement());

        _paymentProcessor.Setup(pp =>
                pp.GetPaymentStatus(It.IsAny<Instance>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(PaymentStatus.Paid);

        var paymentService =
            new PaymentService(_paymentProcessor.Object, _orderDetailsFormatter.Object, _dataService.Object, _logger.Object);

        // Act
        PaymentInformation? result =
            await paymentService.CheckAndStorePaymentInformation(instance, paymentConfiguration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Paid, result.Status);
    }

    [Fact]
    public async Task CancelPayment_ShouldCallCancelAndDelete_WhenPaymentIsNotPaid()
    {
        // Arrange
        Instance instance = CreateInstance();
        AltinnPaymentConfiguration paymentConfiguration = new() { PaymentDataType = "paymentDataType" };
        PaymentInformation paymentInformation = new()
            { Status = PaymentStatus.Created, RedirectUrl = "redirectUrl", PaymentProcessorId = "paymentProcessorId", PaymentReference = "paymentReference" };

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _paymentProcessor.Setup(pp => pp.CancelPayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()))
            .ReturnsAsync(true);

        _dataService.Setup(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Act
        await _paymentService.CancelPaymentIfNotPaid(instance, paymentConfiguration);

        // Assert
        _paymentProcessor.Verify(pp => pp.CancelPayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()), Times.Once);
        _dataService.Verify(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CancelPayment_ShouldNotDelete_WhenPaymentCancellationFails()
    {
        // Arrange
        Instance instance = CreateInstance();
        AltinnPaymentConfiguration paymentConfiguration = new() { PaymentDataType = "paymentDataType" };
        PaymentInformation paymentInformation = new()
            { Status = PaymentStatus.Created, RedirectUrl = "redirectUrl", PaymentProcessorId = "paymentProcessorId", PaymentReference = "paymentReference" };

        _dataService.Setup(ds => ds.GetByType<PaymentInformation>(It.IsAny<Instance>(), It.IsAny<string>()))
            .ReturnsAsync((Guid.NewGuid(), paymentInformation));

        _paymentProcessor.Setup(pp => pp.CancelPayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<PaymentException>(async () => await _paymentService.CancelPaymentIfNotPaid(instance, paymentConfiguration));

        // Act & Assert
        _paymentProcessor.Verify(pp => pp.CancelPayment(It.IsAny<Instance>(), It.IsAny<PaymentInformation>()), Times.Once);
        _dataService.Verify(ds => ds.DeleteById(It.IsAny<InstanceIdentifier>(), It.IsAny<Guid>()), Times.Never);
    }

    private static PaymentInformation CreatePaymentInformation()
    {
        return new PaymentInformation
        {
            RedirectUrl = "Redirect URL",
            PaymentProcessorId = "paymentProcessorId",
            PaymentReference = "PaymentReference",
            Status = PaymentStatus.Created
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
        return new AltinnPaymentConfiguration { PaymentDataType = "paymentInformation" };
    }
}