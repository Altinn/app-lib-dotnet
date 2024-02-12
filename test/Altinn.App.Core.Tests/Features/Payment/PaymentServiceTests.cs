#nullable enable

using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Payment;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;
    private readonly Mock<IPaymentProcessor> _paymentProcessor = new();
    private readonly Mock<IOrderDetailsFormatter> _orderDetailsFormatter = new();
    private readonly Mock<IDataService> _dataService = new();
    private readonly Mock<IProcessReader> _processReader = new();

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService(_paymentProcessor.Object, _orderDetailsFormatter.Object, _dataService.Object, _processReader.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _paymentProcessor.VerifyNoOtherCalls();
        _orderDetailsFormatter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartPayment_StartsPayment()
    {
        // Arrange
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
        
        OrderDetails order = new OrderDetails()
        {
            Currency = "NOK",
            OrderLines = new List<PaymentOrderLine>
            {
                new()
                {
                    Id = "001",
                    Name = "Fee",
                    PriceExVat = 1000,
                    VatPercent = 25,
                }
            }
        };

        var paymentReference = "124";
        var redirectUrl = "https://example.com";
        
        AltinnPaymentConfiguration paymentConfiguration = new()
        {
            PaymentDataType = "paymentInformation"
        };

        PaymentInformation paymentInformation = new()
        {
            PaymentReference = paymentReference,
            RedirectUrl = redirectUrl,
            OrderDetails = order
        };

        _orderDetailsFormatter.Setup(p => p.GetOrderDetails(instance)).ReturnsAsync(order);
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(paymentInformation);

        // Act
        PaymentInformation paymentInformationResult = await _paymentService.StartPayment(instance, paymentConfiguration);

        // Assert
        paymentInformationResult.RedirectUrl.Should().Be(paymentInformationResult.RedirectUrl);

        // Verify calls
        _orderDetailsFormatter.Verify(p => p.GetOrderDetails(instance), Times.Once);
        _paymentProcessor.Verify(p => p.StartPayment(instance, order), Times.Once);
        _dataService.Verify(dc => dc.InsertJsonObject(
            It.IsAny<InstanceIdentifier>(),
            paymentConfiguration.PaymentDataType,
            It.IsAny<object>()));
        
        VerifyNoOtherCalls();
    }
}