using System.Text;
using Altinn.App.Core.Features.Notifications.SecretProvider;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Altinn.App.Core.Tests.Features.Notifications.SecretProvider;

public class NotificationConditionTokenGeneratorTests
{
    private readonly Mock<INotificationConditionSecretProvider> _secretProviderMock = new(MockBehavior.Strict);

    private NotificationConditionTokenGenerator CreateSut() => new(_secretProviderMock.Object);

    private void SetupSecret(string secret) => _secretProviderMock.Setup(x => x.GetSigningSecret()).Returns(secret);

    private static JsonWebTokenHandler _handler => new();

    private static TokenValidationParameters ValidationParams(string secret) =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

    [Fact]
    public async Task GenerateToken_ProducesValidJwt()
    {
        const string secret = "test-secret-that-is-long-enough-for-hmac";
        SetupSecret(secret);
        var instanceGuid = Guid.NewGuid();

        var token = CreateSut().GenerateToken(instanceGuid);

        var result = await _handler.ValidateTokenAsync(token, ValidationParams(secret));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GenerateToken_JtiClaimMatchesInstanceGuid()
    {
        const string secret = "test-secret-that-is-long-enough-for-hmac";
        SetupSecret(secret);
        var instanceGuid = Guid.NewGuid();

        var token = CreateSut().GenerateToken(instanceGuid);

        var result = await _handler.ValidateTokenAsync(token, ValidationParams(secret));
        Assert.True(result.Claims.TryGetValue(JwtRegisteredClaimNames.Jti, out var jti));
        Assert.Equal(instanceGuid.ToString(), jti?.ToString());
    }

    [Fact]
    public async Task GenerateToken_ExpiresIn31Days()
    {
        const string secret = "test-secret-that-is-long-enough-for-hmac";
        SetupSecret(secret);
        var instanceGuid = Guid.NewGuid();
        var before = DateTime.UtcNow.AddDays(31).AddSeconds(-1); //Unix time has no sub-second precision, give 1s leeway for the test

        var token = CreateSut().GenerateToken(instanceGuid);

        var after = DateTime.UtcNow.AddDays(31).AddSeconds(1); //Unix time has no sub-second precision, give 1s leeway for the test
        var result = await _handler.ValidateTokenAsync(token, ValidationParams(secret));
        var expUnix = (long)result.Claims[JwtRegisteredClaimNames.Exp];
        var exp = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        Assert.InRange(exp, before, after);
    }

    [Fact]
    public void GenerateToken_DifferentInstanceGuids_ProduceDifferentTokens()
    {
        const string secret = "test-secret-that-is-long-enough-for-hmac";
        SetupSecret(secret);

        var token1 = CreateSut().GenerateToken(Guid.NewGuid());
        var token2 = CreateSut().GenerateToken(Guid.NewGuid());

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task GenerateToken_WrongSecret_FailsValidation()
    {
        SetupSecret("correct-secret-that-is-long-enough-ok");
        var instanceGuid = Guid.NewGuid();

        var token = CreateSut().GenerateToken(instanceGuid);

        var result = await _handler.ValidateTokenAsync(token, ValidationParams("wrong-secret-that-is-long-enough-ok"));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GenerateToken_CallsGetSigningSecretOnce()
    {
        const string secret = "test-secret-that-is-long-enough-for-hmac";
        SetupSecret(secret);

        CreateSut().GenerateToken(Guid.NewGuid());

        _secretProviderMock.Verify(x => x.GetSigningSecret(), Times.Once);
    }
}
