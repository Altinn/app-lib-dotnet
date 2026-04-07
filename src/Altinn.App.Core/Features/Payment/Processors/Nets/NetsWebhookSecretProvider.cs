using System.Security.Cryptography;
using System.Text;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Infrastructure.Clients.Secrets;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Payment.Processors.Nets;

/// <summary>
/// Provides rotating webhook secrets used as the shared secret for Nets payment webhook callbacks.
/// The underlying rotating <see cref="AppCode"/> values are derived into Nets-compatible strings
/// (Nets requires the webhook Authorization value to match <c>^[a-zA-Z0-9\-= ]*$</c> and be 8-64 chars long).
/// </summary>
internal interface INetsWebhookSecretProvider
{
    /// <summary>
    /// Gets the current webhook secret to send to Nets when registering a webhook.
    /// </summary>
    string GetSigningSecret();

    /// <summary>
    /// Gets all currently valid webhook secrets to match against an incoming webhook callback.
    /// </summary>
    IReadOnlyList<string> GetValidationSecrets();
}

/// <inheritdoc />
internal sealed class NetsWebhookSecretProvider(IOptionsMonitor<AppCodesSettings> options) : INetsWebhookSecretProvider
{
    public string GetSigningSecret()
    {
        return DeriveWebhookSecret(GetCodes()[0]);
    }

    public IReadOnlyList<string> GetValidationSecrets()
    {
        var codes = GetCodes();
        var result = new string[codes.Count];
        for (int i = 0; i < codes.Count; i++)
        {
            result[i] = DeriveWebhookSecret(codes[i]);
        }
        return result;
    }

    private List<AppCode> GetCodes()
    {
        var codes = options.CurrentValue.PaymentsCallback;
        if (codes is null or { Count: 0 })
            throw new PaymentException(
                "AppCodes:PaymentsCallback is not configured. Ensure the app-codes secret is mounted."
            );
        return codes;
    }

    /// <summary>
    /// Derives a Nets-compatible webhook secret from an <see cref="AppCode"/>.
    /// </summary>
    /// <remarks>
    /// Nets Easy documents that the webhook <c>Authorization</c> value must be between 8 and 64 characters
    /// long and contain only alphanumeric characters. Raw <see cref="AppCode.Code"/> values come from the
    /// rotating app-codes secret and may contain characters outside that set (for example underscores in
    /// base64url-style values). To guarantee Nets accepts the value regardless of how the app-code was
    /// generated, we derive a stable 64-character lowercase hex string by SHA-256 hashing the raw code —
    /// hex is a strict subset of alphanumeric and the length fits within the 8-64 bound. The same
    /// derivation is applied on both the signing side (when registering the webhook with Nets) and the
    /// validation side (when an incoming callback is checked), so the transformation is invisible to
    /// callers.
    /// </remarks>
    private static string DeriveWebhookSecret(AppCode code)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(code.Code), hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
