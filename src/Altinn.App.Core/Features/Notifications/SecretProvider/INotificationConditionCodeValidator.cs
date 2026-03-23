using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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
internal sealed class NotificationConditionCodeValidator(
    INotificationConditionSecretProvider secretProvider,
    ILogger<NotificationConditionCodeValidator> logger
) : INotificationConditionCodeValidator
{
    public async Task<bool> ValidateCode(string? code, Guid instanceGuid)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            logger.LogWarning("Notification condition code validation failed: no code provided.");
            return false;
        }

        try
        {
            var secrets = secretProvider.GetValidationSecrets();
            var handler = new JsonWebTokenHandler();

            foreach (var secret in secrets)
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var result = await handler.ValidateTokenAsync(
                    code,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                    }
                );

                if (!result.IsValid)
                    continue;

                var jtiMatches =
                    result.Claims.TryGetValue(JwtRegisteredClaimNames.Jti, out var jti)
                    && jti?.ToString() == instanceGuid.ToString();

                if (!jtiMatches)
                {
                    logger.LogWarning(
                        "Notification condition code validation failed: jti claim {Jti} does not match instanceGuid {InstanceGuid}.",
                        jti,
                        instanceGuid
                    );
                    return false;
                }

                return true;
            }

            logger.LogWarning(
                "Notification condition code validation failed: token did not match any known secrets for instance {InstanceGuid}.",
                instanceGuid
            );
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification condition code validation failed with an unexpected exception.");
            return false;
        }
    }
}
