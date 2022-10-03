using Altinn.App.Core.Interface;
using System.Security.Cryptography.X509Certificates;

namespace Altinn.App.Core.Infrastructure.Clients.Maskinporten
{
    /// <summary>
    /// Class for retreving a X509 sertificate from key vault registered with the "MaskinportenSettings--EncodedX509" key.
    /// This can be removed when key vault is added as a configuration provider and <![CDATA[IOptions<MaskinportenSettings>]]> is populated
    /// from key vault as well.
    /// </summary>
    public class X509CertificateKeyVaultProvider : IX509CertificateProvider
    {
        private readonly ISecrets _secretsClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="X509CertificateKeyVaultProvider"/> class.
        /// </summary>
        public X509CertificateKeyVaultProvider(ISecrets secretsClient)
        {
            _secretsClient = secretsClient;
        }

        /// <summary>
        /// Gets the certificate from key vault
        /// </summary>
        public async Task<X509Certificate2> GetCertificate()
        {
            var secretKey = "MaskinportenSettings--EncodedX509";
            string secret = await _secretsClient.GetSecretAsync(secretKey);
            if (secret == null)
            {
                throw new ArgumentException($"Unable to fetch cert from key vault with the specified secret {secretKey}.");
            }

            var signingCertificate = new X509Certificate2(
                Convert.FromBase64String(secret),
                string.Empty,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            return signingCertificate;
        }
    }
}
