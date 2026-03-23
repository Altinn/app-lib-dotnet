using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Core.Features.Notifications.SecretProvider;

/// <summary>
/// Generates JWT tokens for use in notification condition endpoints.
/// </summary>
internal interface INotificationConditionTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT token for the given instance, valid for 31 days.
    /// </summary>
    string GenerateToken(Guid instanceGuid, CancellationToken ct = default);
}

/// <inheritdoc />
internal sealed class NotificationConditionTokenGenerator(INotificationConditionSecretProvider secretProvider)
    : INotificationConditionTokenGenerator
{
    /// <inheritdoc />
    public string GenerateToken(Guid instanceGuid, CancellationToken ct = default)
    {
        var secret = secretProvider.GetSigningSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Jti] = instanceGuid.ToString() },
            Expires = DateTime.UtcNow.AddDays(31),
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }
}
