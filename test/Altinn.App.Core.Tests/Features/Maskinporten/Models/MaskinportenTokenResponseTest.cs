using System.Text.Json;
using Altinn.App.Core.Features.Maskinporten.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Maskinporten.Models;

public class MaskinportenTokenResponseTest
{
    [Fact]
    public void ShouldDeserializeFromJsonCorrectly()
    {
        // Arrange
        var accessToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJpdHMtYS1tZSJ9.wLLw4Timcl9gnQvA93RgREz-6S5y1UfzI_GYVI_XVDA";
        var json = $$"""
            {
                "access_token": "{{accessToken}}",
                "token_type": "Bearer",
                "expires_in": 120,
                "scope": "anything"
            }
            """;

        // Act
        var token = JsonSerializer.Deserialize<MaskinportenTokenResponse>(json);

        // Assert
        Assert.NotNull(token);
        token.AccessToken.ToStringUnmasked().Should().Be(accessToken);
        token.TokenType.Should().Be("Bearer");
        token.Scope.Should().Be("anything");
        token.ExpiresIn.Should().Be(120);
    }
}
