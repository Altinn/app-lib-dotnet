using System.Net;
using System.Text.Json;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Moq;
using Moq.Protected;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivInstanceClientTest
{
    private readonly InstanceIdentifier _defaultInstanceIdentifier = new($"12345/{Guid.NewGuid()}");
    private const string MaskinportenToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task GetServiceOwnerAccessToken_ReturnsCorrectToken_BasedOnEnvironment(string environmentName)
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var localTestResponse = "testing123";
        var maskinportenResponse = JwtToken.Parse(MaskinportenToken);
        var httpClient = GetHttpClientWithMockedHandlerFactory(HttpStatusCode.OK, localTestResponse);

        fixture
            .MaskinportenClientMock.Setup(x =>
                x.GetAltinnExchangedToken(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(maskinportenResponse);
        fixture.HostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(environmentName);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await fixture.FiksArkivInstanceClient.GetServiceOwnerAccessToken();

        // Assert
        if (environmentName == "Development")
            Assert.Equal(localTestResponse, result);
        else
            Assert.Equal(MaskinportenToken, result);
    }

    [Fact]
    public async Task GetInstance_ReturnsInstance_ForValidResponse()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var instance = new Instance { Id = _defaultInstanceIdentifier.ToString() };
        var httpClient = GetHttpClientWithMockedHandlerFactory(HttpStatusCode.OK, JsonSerializer.Serialize(instance));
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await fixture.FiksArkivInstanceClient.GetInstance(_defaultInstanceIdentifier);

        // Assert
        Assert.Equal(instance.Id, result.Id);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, null)]
    [InlineData(HttpStatusCode.OK, "invalid-json")]
    public async Task GetInstance_ThrowsException_ForInvalidResponse(HttpStatusCode statusCode, string? content)
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var httpClient = GetHttpClientWithMockedHandlerFactory(statusCode, content);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var record = await Record.ExceptionAsync(
            () => fixture.FiksArkivInstanceClient.GetInstance(_defaultInstanceIdentifier)
        );

        // Assert
        Assert.IsType<PlatformHttpException>(record);
        Assert.Equal(statusCode, ((PlatformHttpException)record).Response.StatusCode);
    }

    private static HttpClient GetHttpClientWithMockedHandler(HttpStatusCode statusCode, string? content = null)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                () =>
                    new HttpResponseMessage(statusCode)
                    {
                        Content = content is not null ? new StringContent(content) : null,
                    }
            );

        return new HttpClient(mockHttpMessageHandler.Object);
    }

    private static Func<HttpClient> GetHttpClientWithMockedHandlerFactory(
        HttpStatusCode statusCode,
        string? content = null
    )
    {
        return () => GetHttpClientWithMockedHandler(statusCode, content);
    }
}
