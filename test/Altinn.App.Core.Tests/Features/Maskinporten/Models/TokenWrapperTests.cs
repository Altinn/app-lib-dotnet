using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Maskinporten.Models;

public class JwtBearerTokenTest
{
    [Fact]
    public void ShouldInitializeCorrectly()
    {
        // Arrange
        var encodedToken = TestHelpers.GetEncodedAccessToken();
        var accessToken = AccessToken.Parse(encodedToken.AccessToken);
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var scope = "test";

        // Act
        var tokenWrapper = new TokenWrapper
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            Scope = scope
        };

        // Assert
        tokenWrapper.AccessToken.Should().Be(accessToken);
        tokenWrapper.ExpiresAt.Should().Be(expiresAt);
        tokenWrapper.Scope.Should().Be(scope);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void ShouldIndicateExpiry(int offset, bool expired)
    {
        // Arrange
        var encodedToken = TestHelpers.GetEncodedAccessToken();
        var accessToken = AccessToken.Parse(encodedToken.AccessToken);
        var tokenWrapper = new TokenWrapper
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddHours(offset),
            Scope = "test"
        };

        // Act
        var isExpired = tokenWrapper.IsExpired();

        // Assert
        isExpired.Should().Be(expired);
    }

    [Fact]
    public void ShouldConvertTo_AccessToken()
    {
        // Arrange
        var encodedToken = TestHelpers.GetEncodedAccessToken();
        var accessToken = AccessToken.Parse(encodedToken.AccessToken);
        var tokenWrapper = new TokenWrapper
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Scope = "test"
        };

        // Act
        AccessToken convertedToken = tokenWrapper;

        // Assert
        convertedToken.Should().Be(accessToken);
    }

    [Fact]
    public void ToString_ShouldMask_AccessToken()
    {
        // Arrange
        var encodedToken = TestHelpers.GetEncodedAccessToken();
        var accessToken = AccessToken.Parse(encodedToken.AccessToken);

        // Act
        var tokenWrapper = new TokenWrapper
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Scope = "test"
        };

        // Assert
        tokenWrapper.AccessToken.ToStringUnmasked().Should().Be(encodedToken.AccessToken);
        tokenWrapper.ToString().Should().NotContain(encodedToken.Components.Signature);
        $"{tokenWrapper}".Should().NotContain(encodedToken.Components.Signature);
    }
}
