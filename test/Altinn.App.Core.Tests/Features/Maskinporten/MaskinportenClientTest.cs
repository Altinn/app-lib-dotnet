using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Exceptions;
using Altinn.App.Core.Features.Maskinporten.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Features.Maskinporten;

public class MaskinportenClientTests
{
    private readonly Mock<IOptionsMonitor<MaskinportenSettings>> _mockOptions;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<MaskinportenClient>> _mockLogger;
    private readonly MaskinportenClient _maskinportenClient;
    private readonly MaskinportenSettings _maskinportenSettings =
        new()
        {
            Authority = "https://maskinporten.dev/",
            ClientId = "test-client-id",
            Key = TestHelpers.JsonWebKeyFactory()
        };

    public MaskinportenClientTests()
    {
        _mockOptions = new Mock<IOptionsMonitor<MaskinportenSettings>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<MaskinportenClient>>();
        _mockOptions.Setup(o => o.CurrentValue).Returns(_maskinportenSettings);

        _maskinportenClient = new MaskinportenClient(
            _mockOptions.Object,
            _mockHttpClientFactory.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void FormattedScopes_FormatsCorrectly()
    {
        MaskinportenClient.FormattedScopes(["a", "b", "c"]).Should().Be("a b c");
        MaskinportenClient.FormattedScopes(["a b", "c"]).Should().Be("a b c");
        MaskinportenClient.FormattedScopes(["a b c"]).Should().Be("a b c");
    }

    [Fact]
    public async Task GenerateAuthenticationPayload_HasCorrectFormat()
    {
        // Arrange
        var jwt = "access-token-content";

        // Act
        var content = MaskinportenClient.GenerateAuthenticationPayload(jwt);
        var parsed = await TestHelpers.ParseFormUrlEncodedContent(content);

        // Assert
        parsed.Count.Should().Be(2);
        parsed["grant_type"].Should().Be("urn:ietf:params:oauth:grant-type:jwt-bearer");
        parsed["assertion"].Should().Be(jwt);
    }

    [Fact]
    public void GenerateJwtGrant_HasCorrectFormat()
    {
        // Arrange
        var scopes = "scope1 scope2";

        // Act
        var jwt = _maskinportenClient.GenerateJwtGrant(scopes);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        // Assert
        parsed.Audiences.Count().Should().Be(1);
        parsed.Audiences.First().Should().Be(_maskinportenSettings.Authority);
        parsed.Issuer.Should().Be(_maskinportenSettings.ClientId);
        parsed.Claims.First(x => x.Type == "scope").Value.Should().Be(scopes);
    }

    [Fact]
    public async Task GetAccessToken_ReturnsAToken()
    {
        // Arrange
        string[] scopes = ["scope1", "scope2"];
        var tokenResponse = new MaskinportenTokenResponse
        {
            AccessToken = "access-token-content",
            ExpiresIn = 120,
            Scope = MaskinportenClient.FormattedScopes(scopes),
            TokenType = "-"
        };
        var mockHandler = TestHelpers.MockHttpMessageHandlerFactory(tokenResponse);
        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await _maskinportenClient.GetAccessToken(scopes);

        // Assert
        result.Should().BeEquivalentTo(tokenResponse, config => config.Excluding(x => x.ExpiresAt));
    }

    [Fact]
    public async Task GetAccessToken_ThrowsExceptionWhenTokenIsExpired()
    {
        // Arrange
        var tokenResponse = new MaskinportenTokenResponse
        {
            AccessToken = "expired-access-token",
            ExpiresIn = MaskinportenClient.TokenExpirationMargin,
            Scope = "-",
            TokenType = "-"
        };

        var scopes = new List<string> { "scope1", "scope2" };
        var mockHandler = TestHelpers.MockHttpMessageHandlerFactory(tokenResponse);

        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        Func<Task> act = async () =>
        {
            await _maskinportenClient.GetAccessToken(scopes);
        };

        // Assert
        await act.Should().ThrowAsync<MaskinportenTokenExpiredException>();
    }

    [Fact]
    public async Task GetAccessToken_UsesCachedTokenIfAvailable()
    {
        // Arrange
        string[] scopes = ["scope1", "scope2"];
        var tokenResponse = new MaskinportenTokenResponse
        {
            AccessToken = "2 minute access token content",
            ExpiresIn = 120,
            Scope = MaskinportenClient.FormattedScopes(scopes),
            TokenType = "-"
        };
        var mockHandler = TestHelpers.MockHttpMessageHandlerFactory(tokenResponse);
        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var token1 = await _maskinportenClient.GetAccessToken(scopes);
        var token2 = await _maskinportenClient.GetAccessToken(scopes);

        // Assert
        token1.Should().BeSameAs(token2);
    }

    [Fact]
    public async Task GetAccessToken_GeneratesNewTokenIfRequired()
    {
        // Arrange
        string[] scopes = ["scope1", "scope2"];
        var tokenResponse = new MaskinportenTokenResponse
        {
            AccessToken = "Very short lived access token",
            ExpiresIn = MaskinportenClient.TokenExpirationMargin + 1,
            Scope = MaskinportenClient.FormattedScopes(scopes),
            TokenType = "-"
        };
        var mockHandler = TestHelpers.MockHttpMessageHandlerFactory(tokenResponse);
        var httpClient = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var token1 = await _maskinportenClient.GetAccessToken(scopes);
        await Task.Delay(1000);
        var token2 = await _maskinportenClient.GetAccessToken(scopes);

        // Assert
        token1.Should().NotBeSameAs(token2);
    }

    [Fact]
    public async Task ParseServerResponse_ThrowsOn_UnsuccessfulStatusCode()
    {
        // Arrange
        var unauthorizedResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent(string.Empty)
        };

        // Act
        Func<Task> act = async () =>
        {
            await MaskinportenClient.ParseServerResponse(unauthorizedResponse);
        };

        // Assert
        await act.Should()
            .ThrowAsync<MaskinportenAuthenticationException>()
            .WithMessage(
                $"Maskinporten authentication failed with status code {(int)unauthorizedResponse.StatusCode} *"
            );
    }

    [Fact]
    public async Task ParseServerResponse_ThrowsOn_InvalidJson()
    {
        // Arrange
        var invalidJsonResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Bad json formatting")
        };

        // Act
        Func<Task> act = async () =>
        {
            await MaskinportenClient.ParseServerResponse(invalidJsonResponse);
        };

        // Assert
        await act.Should()
            .ThrowAsync<MaskinportenAuthenticationException>()
            .WithMessage("Maskinporten replied with invalid JSON formatting: *");
    }

    [Fact]
    public async Task ParseServerResponse_ThrowsOn_DisposedObject()
    {
        // Arrange
        var tokenResponse = new MaskinportenTokenResponse
        {
            AccessToken = "access-token-content",
            ExpiresIn = 120,
            Scope = "scope1 scope2",
            TokenType = "-"
        };
        var validHttpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        // Act
        validHttpResponse.Dispose();
        Func<Task> act = async () =>
        {
            await MaskinportenClient.ParseServerResponse(validHttpResponse);
        };

        // Assert
        await act.Should()
            .ThrowAsync<MaskinportenAuthenticationException>()
            .WithMessage("Authentication with Maskinporten failed: *");
    }
}
