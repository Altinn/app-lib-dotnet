using System.Diagnostics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers.Nets;
using Altinn.App.Core.Features.Payment.Providers.Nets.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Payment.Providers.Nets;

public class NetsPaymentProcessorTests
{
    private readonly Mock<INetsClient> _netsClientMock;
    private readonly IOptions<NetsPaymentSettings> _settings;
    private readonly IOptions<GeneralSettings> _generalSettings;
    private readonly NetsPaymentProcessor _processor;

    public NetsPaymentProcessorTests()
    {
        _netsClientMock = new Mock<INetsClient>();
        _settings = Options.Create(new NetsPaymentSettings
        {
            SecretApiKey = "secret",
            BaseUrl = "baseUrl",
            TermsUrl = "termsUrl",
        });
        _generalSettings = Options.Create(new GeneralSettings());
        _processor = new NetsPaymentProcessor(_netsClientMock.Object, _settings, _generalSettings);
    }

    [Fact]
    public async Task StartPayment_WithValidOrderDetails_ReturnsPaymentInformation()
    {
        // Arrange
        Instance instance = CreateInstance();
        var orderDetails = new OrderDetails
        {
            Currency = "NOK",
            OrderLines = []
        };

        _netsClientMock.Setup(x => x.CreatePayment(It.IsAny<NetsCreatePayment>()))
            .ReturnsAsync(new HttpApiResult<NetsCreatePaymentSuccess>
            {
                Result = new NetsCreatePaymentSuccess
                {
                    HostedPaymentPageUrl = "http://paymenturl.com",
                    PaymentId = "12345"
                }
            });

        // Act
        PaymentInformation result = await _processor.StartPayment(instance, orderDetails);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("12345", result.PaymentReference);
        Assert.Equal("http://paymenturl.com", result.RedirectUrl);
    }

    [Fact]
    public async Task StartPayment_WithValidInstanceAndOrderDetails_ReturnsPaymentInformation()
    {
        // Arrange
        Instance instance = CreateInstance();
        var orderDetails = new OrderDetails
        {
            Currency = "NOK",
            OrderLines =
            [
                new PaymentOrderLine() { Id = "1", Name = "Item 1", Quantity = 1, PriceExVat = 100, VatPercent = 25M },
                new PaymentOrderLine() { Id = "2", Name = "Item 2", Quantity = 2, PriceExVat = 200, VatPercent = 25M }
            ]
        };

        int expectedSum = orderDetails.OrderLines.Sum(x => (int)(x.PriceExVat * 100 * x.Quantity * (1 + (x.VatPercent / 100))));

        _netsClientMock.Setup(x => x.CreatePayment(It.IsAny<NetsCreatePayment>()))
            .ReturnsAsync(new HttpApiResult<NetsCreatePaymentSuccess>
            {
                Result = new NetsCreatePaymentSuccess
                {
                    HostedPaymentPageUrl = "http://paymenturl.com",
                    PaymentId = "12345"
                }
            });

        // Act
        PaymentInformation result = await _processor.StartPayment(instance, orderDetails);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("12345", result.PaymentReference);
        Assert.Equal("http://paymenturl.com", result.RedirectUrl);
        Assert.Equal(PaymentStatus.Created, result.Status);

        _netsClientMock.Verify(
            x => x.CreatePayment(It.Is<NetsCreatePayment>(netsCreatePayment => netsCreatePayment.Order.Amount == expectedSum)), Times.Once);
    }

    [Fact]
    public async Task StartPayment_WithInvalidOrderDetails_ThrowsPaymentException()
    {
        // Arrange
        Instance instance = CreateInstance();
        var orderDetails = new OrderDetails
        {
            Currency = "NOK",
            OrderLines = []
        };

        _netsClientMock.Setup(x => x.CreatePayment(It.IsAny<NetsCreatePayment>()))
            .ReturnsAsync(new HttpApiResult<NetsCreatePaymentSuccess>());

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() => _processor.StartPayment(instance, orderDetails));
    }

    [Fact]
    public async Task CancelPayment_WithValidPaymentReference_CallsNetsClientCancelPayment()
    {
        // Arrange
        Instance instance = CreateInstance();
        PaymentInformation paymentInformation = new()
        {
            RedirectUrl = "redirectUrl",
            PaymentProcessorId = "paymentProcessorId",
            PaymentReference = "paymentReference",
            OrderDetails = new OrderDetails()
            {
                Currency = "NOK",
                OrderReference = "orderReference",
                OrderLines =
                [
                    new PaymentOrderLine
                    {
                        Id = "1",
                        Name = "Item 1",
                        PriceExVat = 500,
                        VatPercent = 25,
                    }
                ],
            },
        };

        _netsClientMock.Setup(x => x.CancelPayment(paymentInformation.PaymentReference, (int)(paymentInformation.OrderDetails.TotalPriceIncVat * 100))).ReturnsAsync(true);

        // Act
        await _processor.CancelPayment(instance, paymentInformation);

        // Assert
        _netsClientMock.Verify(x => x.CancelPayment(paymentInformation.PaymentReference, (int)paymentInformation.OrderDetails.TotalPriceIncVat * 100), Times.Once);
    }

    [Fact]
    public async Task GetPaymentStatus_WithValidPaymentReferenceAndExpectedTotal_ReturnsPaymentStatus()
    {
        // Arrange
        Instance instance = CreateInstance();
        const string paymentReference = "12345";
        const decimal expectedTotalIncVat = 100;

        _netsClientMock.Setup(x => x.RetrievePayment(paymentReference))
            .ReturnsAsync(new HttpApiResult<NetsPaymentFull>
            {
                Result = new NetsPaymentFull
                {
                    Payment = new NetsPayment
                    {
                        Summary = new NetsSummary
                        {
                            // All amounts sent to and received from Nets are in the lowest monetary unit for the given currency, without punctuation marks.
                            ChargedAmount = expectedTotalIncVat * 100
                        }
                    }
                }
            });

        // Act
        PaymentStatus? result = await _processor.GetPaymentStatus(instance, paymentReference, expectedTotalIncVat);

        // Assert
        Assert.Equal(PaymentStatus.Paid, result);
    }

    [Fact]
    public async Task GetPaymentStatus_WithInvalidPaymentReference_ThrowsPaymentException()
    {
        // Arrange
        Instance instance = CreateInstance();
        const string paymentReference = "12345";
        const decimal expectedTotalIncVat = 100;

        _netsClientMock.Setup(x => x.RetrievePayment(paymentReference))
            .ReturnsAsync(new HttpApiResult<NetsPaymentFull>());

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() => _processor.GetPaymentStatus(instance, paymentReference, expectedTotalIncVat));
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