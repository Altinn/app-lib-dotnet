using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Models;

public class AccessTokenTests
{
    private static readonly string[] _validTokens =
    [
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiSm9obiBEb2UifQ.DjwRE2jZhren2Wt37t5hlVru6Myq4AhpGLiiefF69u8"
    ];
    private static readonly string _invalidToken = "is.not.base64token";

    [Fact]
    public void Parse_ValidToken_ShouldReturnAccessToken()
    {
        // Arrange
        var encodedToken = _validTokens[0];

        // Act
        var accessToken = AccessToken.Parse(encodedToken);

        // Assert
        accessToken.Value.Should().Be(encodedToken);
    }

    [Fact]
    public void Parse_InvalidToken_ShouldThrowFormatException()
    {
        Assert.Throws<FormatException>(() => AccessToken.Parse(_invalidToken));
    }

    [Fact]
    public void Equals_SameToken_ShouldReturnTrue()
    {
        // Arrange
        var token1 = AccessToken.Parse(_validTokens[0]);
        var token2 = AccessToken.Parse(_validTokens[0]);

        // Act
        bool result1 = token1.Equals(token2);
        bool result2 = token1 == token2;
        bool result3 = token1 != token2;

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentToken_ShouldReturnFalse()
    {
        // Arrange
        var token1 = AccessToken.Parse(_validTokens[0]);
        var token2 = AccessToken.Parse(_validTokens[1]);

        // Act
        bool result1 = token1.Equals(token2);
        bool result2 = token1 == token2;
        bool result3 = token1 != token2;

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnMaskedToken()
    {
        // Arrange
        var token = AccessToken.Parse(_validTokens[0]);

        // Act
        var maskedToken1 = token.ToString();
        var maskedToken2 = $"{token}";

        // Assert
        maskedToken1.Should().Be(maskedToken2);
        maskedToken1
            .Should()
            .Be(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.<masked>"
            );
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnFullTokenString()
    {
        // Arrange
        var token = AccessToken.Parse(_validTokens[0]);

        // Act
        string tokenString = token;

        // Assert
        tokenString.Should().Be(_validTokens[0]);
    }

    [Fact]
    public void GetMethod_ShouldReturnFullTokenString()
    {
        // Arrange
        var token = AccessToken.Parse(_validTokens[0]);

        // Act
        string tokenString = token.Value;

        // Assert
        tokenString.Should().Be(_validTokens[0]);
    }
}
