using System.Net;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Core.Models;
using Moq;
using Moq.Protected;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivInstanceClientTest
{
    private const string MaskinportenToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task GetServiceOwnerAccessToken_ReturnsCorrectToken_BasedOnEnvironment(string environmentName)
    {
        // Arrange
        using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var localTestResponse = "testing123";
        var maskinportenResponse = JwtToken.Parse(MaskinportenToken);
        var httpClient = GetHttpClientWithMockedHandler(GetResponseMessage(HttpStatusCode.OK, localTestResponse));

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

    private HttpResponseMessage GetResponseMessage(HttpStatusCode statusCode, string? content = null)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = content is not null ? new StringContent(content) : null,
        };
    }

    private HttpClient GetHttpClientWithMockedHandler(HttpResponseMessage responseMessage)
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => responseMessage);

        return new HttpClient(mockHttpMessageHandler.Object);
    }
}
