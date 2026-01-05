using System.Text.Json;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Processors;
using Altinn.App.Core.Features.Payment.Processors.Nets;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Tests.Common.Fixtures;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class PaymentControllerTests
{
    private static readonly Guid _instanceGuid = Guid.Parse("00000000-DEAD-FACE-BABE-000000000001");
    private const int PartyId = 123456;
    private readonly MockedServiceCollection _services = new();

    private readonly Instance _instance = new Instance()
    {
        Id = $"{PartyId}/{_instanceGuid}",
        InstanceOwner = new InstanceOwner() { PartyId = $"{PartyId}" },
        Process = new()
        {
            CurrentTask = new() { ElementId = "currentTask", AltinnTaskType = AltinnTaskTypes.Payment },
        },
    };

    private readonly NetsPaymentSettings _netsPaymentSettings = new()
    {
        BaseUrl = null!,
        TermsUrl = null!,
        SecretApiKey = "secure",
    };

    private readonly OrderDetails _orderDetails = new OrderDetails
    {
        PaymentProcessorId = "Nets Easy",
        OrderLines =
        [
            new()
            {
                Id = "line1",
                Name = "Product 1",
                Quantity = 1,
                Unit = "pcs",
                PriceExVat = 100,
                VatPercent = 25,
            },
        ],
        Receiver = new() { },
        Currency = "NOK",
    };

    public PaymentControllerTests(ITestOutputHelper outputHelper)
    {
        _services.OutputHelper = outputHelper;
        // Ensure common mocks that are dependencies of payment controller/service is registered
        _services.Mock<IProcessReader>();

        // Register tested services as singletons
        _services.Services.AddSingleton<IPaymentService, PaymentService>();
        _services.Services.AddSingleton<PaymentController>();

        _services.Services.AddSingleton(Options.Create(_netsPaymentSettings));

        // Add default instance to mocked storage
        _services.Storage.AddInstance(_instance);
        _services.Storage.AppMetadata.DataTypes.Add(
            new()
            {
                Id = "paymentDataType",
                AllowedContentTypes = new List<string> { "application/json" },
            }
        );
    }

    [Fact]
    public async Task GetPaymentInformation_NoCurrentTask_ReturnsBadRequest()
    {
        _instance.Process.CurrentTask = null;

        await using var sp = _services.BuildServiceProvider();

        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.GetPaymentInformation("org", "app", PartyId, _instanceGuid);
        Assert.IsType<BadRequestObjectResult>(result);

        _services.VerifyMocks();
    }

    [Fact]
    public async Task GetPaymentInformation_TaskIsNotPaymentTask_ReturnsBadRequest()
    {
        SetupAltinnTaskExtensionMock("currentTask", null, Times.Once());
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.GetPaymentInformation("org", "app", PartyId, _instanceGuid);
        Assert.IsType<BadRequestObjectResult>(result);
        _services.VerifyMocks();
    }

    [Fact]
    public async Task GetPaymentInformation_PaymentTaskWithoutPaymentInfo_DoesNotCreateNewPayment()
    {
        SetupAltinnTaskExtensionMock(
            "currentTask",
            new AltinnTaskExtension()
            {
                PaymentConfiguration = new()
                {
                    PaymentDataType = "paymentDataType",
                    PaymentReceiptPdfDataType = "paymentPdfDataType",
                },
            },
            Times.Once()
        );
        SetupOrderDetailsCalculatorMock(PartyId, _instanceGuid, _orderDetails, Times.Once());
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        // Act
        var result = await controller.GetPaymentInformation("org", "app", PartyId, _instanceGuid);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        var (instance, _) = _services.Storage.GetInstanceAndData(PartyId, _instanceGuid);

        // Ensure that no payment object is created. Maybe it should?
        Assert.DoesNotContain(instance.Data, d => d.DataType == "paymentDataType");

        _services.VerifyMocks();
    }

    [Fact]
    public async Task GetOrderDetails_ReturnsCalculatedOrderDetails()
    {
        SetupOrderDetailsCalculatorMock(PartyId, _instanceGuid, _orderDetails, Times.Once());

        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();

        // Act
        var result = await controller.GetOrderDetails("org", "app", PartyId, _instanceGuid);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualOrderDetails = Assert.IsType<OrderDetails>(okResult.Value);
        Assert.Equal(_orderDetails.PaymentProcessorId, actualOrderDetails.PaymentProcessorId);
        Assert.Equal(_orderDetails.OrderLines.Count, actualOrderDetails.OrderLines.Count);
        Assert.Equal(_orderDetails.Currency, actualOrderDetails.Currency);

        _services.VerifyMocks();
    }

    [Fact]
    public async Task GetOrderDetails_NoOrderDetailsCalculatorRegistrerd_ThrowsException()
    {
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PaymentException>(async () =>
            await controller.GetOrderDetails("org", "app", PartyId, _instanceGuid)
        );

        Assert.Contains("IOrderDetailsCalculator", exception.Message);
    }

    [Fact]
    public async Task PaymentWebhookListener_NoWebhookCallbackKeyFromConfig_ReturnsNotFound()
    {
        _netsPaymentSettings.WebhookCallbackKey = null!;
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer somekey"
        );
        var response = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("no WebhookCallbackKey is configured", response.Value?.ToString()!);
        _services.VerifyMocks();
    }

    [Fact]
    public async Task PaymentWebhookListener_InvalidAuthorization_ReturnsUnauthorized()
    {
        var callbackKey = "validkey";
        var wrongKey = "invalid";
        _netsPaymentSettings.WebhookCallbackKey = callbackKey;
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer " + wrongKey
        );
        Assert.IsType<UnauthorizedObjectResult>(result);
        _services.VerifyMocks();
    }

    [Fact]
    public async Task PaymentWebhookListener_NoCurrentTask_ReturnsBadRequest()
    {
        _instance.Process.CurrentTask = null;
        var callbackKey = "invalid";
        _netsPaymentSettings.WebhookCallbackKey = callbackKey;

        await using var sp = _services.BuildServiceProvider();

        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer " + callbackKey
        );
        Assert.IsType<BadRequestObjectResult>(result);

        _services.VerifyMocks();
    }

    [Fact]
    public async Task PaymentWebhookListener_TaskIsNotPaymentTask_ReturnsOkRequest()
    {
        var callbackKey = "validkey";
        _netsPaymentSettings.WebhookCallbackKey = callbackKey;
        SetupAltinnTaskExtensionMock(
            "currentTask",
            new AltinnTaskExtension() { PaymentConfiguration = null },
            Times.Once()
        );
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer " + callbackKey
        );
        var response = Assert.IsType<OkObjectResult>(result);
        var responseString = JsonSerializer.Serialize(response.Value);
        Assert.Contains("Payment configuration not found", responseString);
        _services.VerifyMocks();
    }

    [Fact]
    public async Task PaymentWebhookListener_ValidRequestWithPaymentInfo_ReturnsOkRequest()
    {
        var callbackKey = "validkey";
        _netsPaymentSettings.WebhookCallbackKey = callbackKey;
        SetupAltinnTaskExtensionMock(
            "currentTask",
            new AltinnTaskExtension()
            {
                PaymentConfiguration = new()
                {
                    PaymentDataType = "paymentDataType",
                    PaymentReceiptPdfDataType = "paymentPdfDataType",
                },
            },
            Times.Once()
        );

        _services
            .Mock<IPaymentProcessor>()
            .SetupGet(pp => pp.PaymentProcessorId)
            .Returns("Nets Easy")
            .Verifiable(Times.AtLeastOnce());

        _services
            .Mock<IPaymentProcessor>()
            .Setup(pp =>
                pp.GetPaymentStatus(It.IsAny<Instance>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>())
            )
            .ReturnsAsync((PaymentStatus.Paid, new PaymentDetails { PaymentId = "some-payment-id" }))
            .Verifiable(Times.Once());

        _services.Storage.AddData(
            _instance,
            "paymentDataType",
            "application/json",
            JsonSerializer.SerializeToUtf8Bytes(
                new PaymentInformation()
                {
                    TaskId = "currentTask",
                    Status = PaymentStatus.Created,
                    OrderDetails = _orderDetails,
                    PaymentDetails = new PaymentDetails { PaymentId = "some-payment-id" },
                }
            )
        );

        // The callback currently triggers a process advance, so we need to mock that endpoint
        // In the future we might want to call the process engine directly instead of via HTTP
        _services.FakeHttpMessageHandler.RegisterJsonEndpoint(
            HttpMethod.Put,
            $"/{_instance.AppId}/instances/{_instance.Id}/process/next",
            new AppProcessState()
        );

        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer " + callbackKey
        );
        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Payment status is Paid for instance 12345", response.Value?.ToString());
        _services.VerifyMocks();
    }

    [Fact]
    public async Task PaymentWebhookListener_ValidRequestWithoutPaymentInfo_ReturnsOkRequest()
    {
        var callbackKey = "validkey";
        _netsPaymentSettings.WebhookCallbackKey = callbackKey;
        SetupAltinnTaskExtensionMock(
            "currentTask",
            new AltinnTaskExtension()
            {
                PaymentConfiguration = new()
                {
                    PaymentDataType = "paymentDataType",
                    PaymentReceiptPdfDataType = "paymentPdfDataType",
                },
            },
            Times.Once()
        );
        await using var sp = _services.BuildServiceProvider();
        var controller = sp.GetRequiredService<PaymentController>();
        var result = await controller.PaymentWebhookListener(
            "org",
            "app",
            PartyId,
            _instanceGuid,
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Data = new() { PaymentId = Guid.NewGuid().ToString() },
                EventName = "PaymentCreated",
                Timestamp = DateTime.UtcNow,
                MerchantId = 222,
            },
            "Bearer " + callbackKey
        );
        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("No payment information stored yet for instance", response.Value?.ToString());
        _services.VerifyMocks();
    }

    private void SetupAltinnTaskExtensionMock(string taskId, AltinnTaskExtension? taskExtension, Times times)
    {
        _services
            .Mock<IProcessReader>()
            .Setup(pr => pr.GetAltinnTaskExtension(taskId))
            .Returns(taskExtension)
            .Verifiable(times);
    }

    private void SetupOrderDetailsCalculatorMock(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        OrderDetails orderDetails,
        Times times
    )
    {
        var instanceId = $"{instanceOwnerPartyId}/{instanceGuid}";
        _services
            .Mock<IOrderDetailsCalculator>()
            .Setup(odc => odc.CalculateOrderDetails(It.Is<Instance>(i => i.Id == instanceId), It.IsAny<string?>()))
            .ReturnsAsync(orderDetails)
            .Verifiable(times);
    }
}
