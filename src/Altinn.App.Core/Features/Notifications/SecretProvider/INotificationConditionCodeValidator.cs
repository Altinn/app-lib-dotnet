using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Altinn.App.Core.Features.Notifications.SecretProvider;

/// <summary>
/// Validates JWT codes used for notification condition endpoints.
/// </summary>
public interface INotificationConditionCodeValidator
{
    /// <summary>
    /// Validates that the provided code is a valid JWT signed with the notification condition secret,
    /// and that it was issued for the specified instance.
    /// </summary>
    Task<bool> ValidateCode(string? code, Guid instanceGuid);
}

/// <inheritdoc />
internal sealed class NotificationConditionCodeValidator(INotificationConditionSecretProvider secretProvider)
    : INotificationConditionCodeValidator
{
    /// <inheritdoc />
    public async Task<bool> ValidateCode(string? code, Guid instanceGuid)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var secret = await secretProvider.GetSecretCode();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var handler = new JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(code, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            });

            if (!result.IsValid)
                return false;

            return result.Claims.TryGetValue(JwtRegisteredClaimNames.Jti, out var jti)
                && jti?.ToString() == instanceGuid.ToString();
        }
        catch
        {
            return false;
        }
    }
}
