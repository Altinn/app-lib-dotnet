using Microsoft.Extensions.Configuration;

namespace Altinn.App.Core.Features.Notifications.SecretProvider;

/// <summary>
/// Provides a secret used for signing and validating notification condition endpoint JWT tokens.
/// </summary>
internal interface INotificationConditionSecretProvider
{
    /// <summary>
    /// Gets the secret used for signing JWT tokens for notification condition endpoints.
    /// </summary>
    Task<string> GetSecretCode();
}

/// <inheritdoc />
internal sealed class NotificationConditionSecretProvider(IConfiguration configuration)
    : INotificationConditionSecretProvider
{
    /// <inheritdoc />
    public Task<string> GetSecretCode()
    {
        var secret =
            configuration["NotificationConditionSecret"]
            ?? throw new InvalidOperationException(
                "NotificationConditionSecret is not configured. Add it to your app configuration or keyvault."
            );
        return Task.FromResult(secret);
    }
}
