using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Response from the wallet verification status endpoint.
/// </summary>
public class VerificationStatusResponse
{
    /// <summary>
    /// The current status of the verification.
    /// Possible values: PENDING, AVAILABLE, FAILED
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }
}
