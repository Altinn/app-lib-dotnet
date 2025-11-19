using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Response from the wallet verification result endpoint containing the verified credential claims.
/// </summary>
public class VerificationResultResponse
{
    /// <summary>
    /// The verified claims data from the wallet credential.
    /// </summary>
    [JsonPropertyName("claims")]
    public required WalletClaimsData Claims { get; init; }
}
