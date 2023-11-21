#nullable enable

using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Payment;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService;
    private readonly Mock<IOrderDetailsFormatter> _orderDetails = new();
    private readonly Mock<IPaymentProcessor> _paymentProcessor = new();
    private readonly Mock<IDataClient> _dataClient = new();

    public PaymentServiceTests()
    {
        _paymentService = new PaymentService(_orderDetails.Object, _paymentProcessor.Object, _dataClient.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _orderDetails.VerifyNoOtherCalls();
        _paymentProcessor.VerifyNoOtherCalls();
        _dataClient.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task StartPayment_StartsPayment()
    {
        // Arrange
        Instance instance = new Instance();
        PaymentOrder order = new PaymentOrder()
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
        PaymentStartResult reference = new()
        {
            PaymentReference = "124",
            RedirectUrl = "https://example.com",
        };
        _orderDetails.Setup(o => o.GetOrderDetails(instance)).ReturnsAsync(order);
        _paymentProcessor.Setup(p => p.StartPayment(instance, order)).ReturnsAsync(reference);
        
        // Act
        var result = await _paymentService.StartPayment(instance);
        
        // Assert
        result.RedirectUrl.Should().Be(reference.RedirectUrl);
        
        // Verify calls
        _orderDetails.Verify(o => o.GetOrderDetails(instance), Times.Once);
        _paymentProcessor.Verify(p => p.StartPayment(instance, order), Times.Once);
        _dataClient.Verify(d => d.InsertBinaryData(It.IsAny<string>(), "payment-reference", "application/text", "payment-reference", It.IsAny<Stream>(), It.IsAny<string?>()), Times.Once);
        VerifyNoOtherCalls();
    }
}