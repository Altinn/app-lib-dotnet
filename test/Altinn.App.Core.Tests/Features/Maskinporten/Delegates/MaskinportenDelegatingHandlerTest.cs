using System.Net;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Delegates;
using Altinn.App.Core.Features.Maskinporten.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Altinn.App.Core.Tests.Features.Maskinporten.Tests.Delegates;

public class MaskinportenDelegatingHandlerTest
{
    [Fact]
    public async Task SendAsync_AddsAuthorizationHeader()
    {
        // Arrange
        var scopes = new List<string> { "scope1", "scope2" };
        var mockProvider = new Mock<IServiceProvider>();
        var innerHandlerMock = new Mock<DelegatingHandler>();
        var mockLogger = new Mock<ILogger<MaskinportenDelegatingHandler>>();
        var mockMaskinportenClient = new Mock<IMaskinportenClient>();

        var tokenResponse = new MaskinportenTokenResponse
        {
            TokenType = "-",
            Scope = "-",
            AccessToken = "jwt-content-placeholder",
            ExpiresIn = -1
        };

        mockProvider
            .Setup(p => p.GetService(typeof(ILogger<MaskinportenDelegatingHandler>)))
            .Returns(mockLogger.Object);
        mockProvider.Setup(p => p.GetService(typeof(IMaskinportenClient))).Returns(mockMaskinportenClient.Object);

        innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        mockMaskinportenClient
            .Setup(c => c.GetAccessToken(scopes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        var handler = new MaskinportenDelegatingHandler(scopes, mockProvider.Object, mockLogger.Object)
        {
            InnerHandler = innerHandlerMock.Object
        };

        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://unittesting.to.nowhere");

        // Act
        await httpClient.SendAsync(request);

        // Assert
        mockMaskinportenClient.Verify(c => c.GetAccessToken(scopes, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(request.Headers.Authorization);
        request.Headers.Authorization.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be(tokenResponse.AccessToken);
    }
}
