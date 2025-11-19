using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Response from the wallet verification start endpoint.
/// </summary>
public class VerificationStartResponse
{
    /// <summary>
    /// The unique verification transaction ID.
    /// </summary>
    [JsonPropertyName("verifier_transaction_id")]
    public required string VerifierTransactionId { get; init; }

    /// <summary>
    /// The authorization request URL to redirect the user to their wallet app.
    /// </summary>
    [JsonPropertyName("authorization_request")]
    public required string AuthorizationRequest { get; init; }
}
