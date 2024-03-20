namespace Altinn.App.Core.Tests.Infrastructure.Clients.Notifications.Sms;

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Infrastructure.Clients.Notifications.Sms;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Notifications.Sms;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Common.AccessTokenClient.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

public class SmsNotificationClientTests
{
    [Fact]
    public async void Order_VerifyHttpCall()
    {
        // Arrange
        var smsNotification = new SmsNotification
        {
            SenderNumber = "+4799999999",
            Body = "body",
            Recipients = [new("test.testesen@testdirektoratet.no")],
            SendersReference = "testref",
            RequestedSendTime = DateTime.UtcNow,
        };

        var expectedUri = "http://localhost:5101/notifications/api/v1/orders/sms";
        var expectedContent = JsonSerializer.Serialize(smsNotification);

        HttpRequestMessage? capturedRequest = null; // Capture request to verify the uri used in the http call.
        string capturedContent = string.Empty; // Capture http content to verify.

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"orderId\": \"order123\"}", Encoding.UTF8, "application/json")
            })
            .Callback<HttpRequestMessage, CancellationToken>(async (request, token) =>
            {
                capturedRequest = request;
                capturedContent = await request.Content!.ReadAsStringAsync(token);
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(handlerMock.Object);

        httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var smsNotificationClient = CreateSmsNotificationClient(httpClientFactoryMock);

        // Act
        _ = await smsNotificationClient.Order(smsNotification, default);

        // Assert
        capturedContent.Should().Be(expectedContent);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be(expectedUri);
    }

    [Fact]
    public async void Order_ShouldReturnOrderId_OnSuccess()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var jsonContent = new StringContent("{\"orderId\": \"order123\"}", Encoding.UTF8, "application/json");
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = jsonContent,
                };
                return response;
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(handlerMock.Object);

        httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var smsNotificationClient = CreateSmsNotificationClient(httpClientFactoryMock);
        var recipients = new List<SmsRecipient>()
        {
            new("test.testesen@testdirektoratet.no")
        };

        var smsNotification = new SmsNotification
        {
            SenderNumber = "+4799999999",
            Body = "body",
            Recipients = recipients,
            SendersReference = "testref",
            RequestedSendTime = DateTime.UtcNow,
        };

        // Act
        var smsOrderResponse = await smsNotificationClient.Order(smsNotification, default);

        // Assert
        smsOrderResponse.Should().NotBeNull();
        smsOrderResponse.OrderId.ToString().Should().BeEquivalentTo("order123");
    }

    [Fact]
    public async void Order_ShouldThrowSmsNotificationException_OnFailure()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var jsonContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = jsonContent,
                };
                return response;
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient(handlerMock.Object);

        httpClientFactoryMock.Setup(h => h.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var smsNotificationClient = CreateSmsNotificationClient(httpClientFactoryMock);
        var recipients = new List<SmsRecipient>()
        {
            new("test.testesen@testdirektoratet.no")
        };

        var smsNotification = new SmsNotification
        {
            SenderNumber = "+4799999999",
            Body = "body",
            Recipients = recipients,
            SendersReference = "testref",
            RequestedSendTime = DateTime.UtcNow,
        };

        // Act
        Func<Task> orderSmsNotification = async () => await smsNotificationClient.Order(smsNotification, default);

        // Assert
        await FluentActions.Awaiting(orderSmsNotification).Should().ThrowAsync<SmsNotificationException>();
    }

    private static SmsNotificationClient CreateSmsNotificationClient(Mock<IHttpClientFactory> mockHttpClientFactory)
    {
        var loggerFactory = new NullLoggerFactory();

        var appDataMock = new Mock<IAppMetadata>();
        appDataMock.Setup(a => a.GetApplicationMetadata())
            .ReturnsAsync(new ApplicationMetadata("ttd/app-lib-test"));

        var accessTokenGenerator = new Mock<IAccessTokenGenerator>();
        accessTokenGenerator.Setup(a => a.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("token");

        var sp = new ServiceCollection().BuildServiceProvider();

        return new SmsNotificationClient(
            loggerFactory.CreateLogger<SmsNotificationClient>(), 
            mockHttpClientFactory.Object,
            Options.Create(new PlatformSettings()),
            appDataMock.Object,
            accessTokenGenerator.Object,
            sp);
    }
}
