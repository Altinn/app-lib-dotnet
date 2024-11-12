using Altinn.App.Api.Tests.Utils;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Models;
using FluentAssertions;
using Moq;

namespace Altinn.App.Core.Tests.Features.Maskinporten.Tests.Delegates;

public class MaskinportenDelegatingHandlerTest
{
    [Fact]
    public async Task SendAsync_AddsAuthorizationHeader()
    {
        // Arrange
        var scopes = new[] { "scope1", "scope2" };
        var accessToken = PrincipalUtil.GetMaskinportenToken(scope: "-").AccessToken;
        var (client, handler) = TestHelpers.MockMaskinportenDelegatingHandlerFactory(
            TokenAuthorities.Maskinporten,
            scopes,
            new TokenWrapper
            {
                Scope = "-",
                AccessToken = accessToken,
                ExpiresAt = DateTime.MinValue
            }
        );
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://some-maskinporten-url/token");

        // Act
        await httpClient.SendAsync(request);

        // Assert
        client.Verify(c => c.GetAccessToken(scopes, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(request.Headers.Authorization);
        request.Headers.Authorization.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be(accessToken.ToStringUnmasked());
    }
}
