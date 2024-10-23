using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Features.Maskinporten.Models;
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
        var (client, handler) = TestHelpers.MockMaskinportenDelegatingHandlerFactory(
            TokenAuthorities.Maskinporten,
            scopes,
            new JwtBearerToken
            {
                Scope = "-",
                AccessToken = "jwt-content-placeholder",
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
        request.Headers.Authorization.Parameter.Should().Be("jwt-content-placeholder");
    }
    //
    // [Fact]
    // public async Task SendAsync_OnlyAccepts_BearerTokens()
    // {
    //     // Arrange
    //     var (_, handler) = TestHelpers.MockMaskinportenDelegatingHandlerFactory(
    //         TokenAuthority.Maskinporten,
    //         ["scope1", "scope2"],
    //         new MaskinportenAccessToken
    //         {
    //             TokenType = "MAC",
    //             Scope = "-",
    //             AccessToken = "jwt-content-placeholder",
    //             ExpiresIn = -1
    //         }
    //     );
    //     var httpClient = new HttpClient(handler);
    //     var request = new HttpRequestMessage(HttpMethod.Get, "https://some-maskinporten-url/token");
    //
    //     // Act
    //     Func<Task> act = async () => await httpClient.SendAsync(request);
    //
    //     // Assert
    //     await act.Should()
    //         .ThrowAsync<MaskinportenUnsupportedTokenException>()
    //         .WithMessage("Unsupported token type received from Maskinporten: *");
    // }
}
