#nullable enable

using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Payment;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Payment;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;
    private readonly Mock<IPaymentProcessor> _paymentProcessor = new();
    private readonly Mock<IOrderDetailsFormatter> _orderDetailsFormatter = new();

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService(_paymentProcessor.Object, _orderDetailsFormatter.Object);
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
        Instance instance = new Instance();
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

        PaymentInformation paymentInformation = new()
        {
            PaymentReference = paymentReference,
            RedirectUrl = redirectUrl,
            OrderDetails = order
        };

        _orderDetailsFormatter.Setup(p => p.GetOrderDetails(instance)).ReturnsAsync(order);
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(paymentInformation);

        // Act
        PaymentInformation paymentInformationResult = await _paymentService.StartPayment(instance);

        // Assert
        paymentInformationResult.RedirectUrl.Should().Be(paymentInformationResult.RedirectUrl);

        // Verify calls
        _paymentProcessor.Verify(p => p.StartPayment(instance, order), Times.Once);
        _orderDetailsFormatter.Verify(p => p.GetOrderDetails(instance), Times.Once);
        VerifyNoOtherCalls();
    }
}