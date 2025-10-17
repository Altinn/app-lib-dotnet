using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

/// <summary>
/// Represents the settings for a FIKS IO account.
/// </summary>
public sealed record FiksIOSettings : IFiksIOAccountSettings
{
    /// <inheritdoc />
    [Required]
    [JsonPropertyName("accountId")]
    public required Guid AccountId { get; set; }

    /// <inheritdoc />
    [Required]
    [JsonPropertyName("integrationId")]
    public required Guid IntegrationId { get; set; }

    /// <summary>
    /// The integration password.
    /// </summary>
    [Required]
    [JsonPropertyName("integrationPassword")]
    public required string IntegrationPassword { get; set; }

    /// <summary>
    /// The account private key, base64 encoded.
    /// </summary>
    /// <remarks>
    /// This is the private key used to authenticate the Fiks IO account, and to decrypt incoming messages.
    /// The corresponding public key must be registered with the Fiks platform.
    /// The value should be in base64 encoded PEM format, including header and footer.
    /// </remarks>
    [Required]
    [JsonPropertyName("accountPrivateKeyBase64")]
    public required string AccountPrivateKeyBase64 { get; set; }

    internal string AccountPrivateKey => Encoding.UTF8.GetString(Convert.FromBase64String(AccountPrivateKeyBase64));
}
