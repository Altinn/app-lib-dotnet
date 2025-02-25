using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn.App.Clients.Fiks.FiksIO;

public sealed record FiksIOSettings
{
    [Required]
    [JsonPropertyName("accountId")]
    public required Guid AccountId { get; set; }

    [Required]
    [JsonPropertyName("integrationId")]
    public required Guid IntegrationId { get; set; }

    [Required]
    [JsonPropertyName("integrationPassword")]
    public required string IntegrationPassword { get; set; }

    [Required]
    [JsonPropertyName("accountPrivateKeyBase64")]
    public required string AccountPrivateKeyBase64 { get; set; }

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
