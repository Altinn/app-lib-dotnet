using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    /// This is the private key used to authenticate the Fiks IO account.
    /// The corresponding public key must be registered with the Fiks platform.
    /// The value should be in base64 encoded PEM format, including header and footer.
    /// </remarks>
    [Required]
    [JsonPropertyName("accountPrivateKeyBase64")]
    public required string AccountPrivateKeyBase64 { get; set; }

    /// <summary>
    /// The ASiC-E private key, base64 encoded.
    /// </summary>
    /// <remarks>
    /// This is the key used for end-to-end message encryption. The public part of this key will be used by senders
    /// to encrypt messages sent to this account. For this reason, the key should not be rotated while there are pending
    /// messages in the inbound queue.
    /// The value should be in base64 encoded PEM format, including header and footer.
    /// </remarks>
    [Required]
    [JsonPropertyName("asicePrivateKeyBase64")]
    public required string AsicePrivateKeyBase64 { get; set; }

    internal string AccountPrivateKey => Encoding.UTF8.GetString(Convert.FromBase64String(AccountPrivateKeyBase64));
    internal string AsicePrivateKey => Encoding.UTF8.GetString(Convert.FromBase64String(AsicePrivateKeyBase64));

    internal X509Certificate2 GenerateAsiceCertificate()
    {
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(AsicePrivateKey);

        var subject = new X500DistinguishedName($"CN={Guid.NewGuid()}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-5), // Don't want to get stuck on a clock issue here...
            DateTimeOffset.UtcNow.AddYears(5)
        );

        return certificate;
    }
}
