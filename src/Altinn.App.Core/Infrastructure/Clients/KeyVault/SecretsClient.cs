using Altinn.App.Core.Internal.Secrets;
using AltinnCore.Authentication.Constants;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.KeyVault.WebKey;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Altinn.App.Core.Infrastructure.Clients.KeyVault
{
    /// <summary>
    /// Class that handles integration with Azure Key Vault
    /// </summary>
    public class SecretsClient : ISecretsClient, IDisposable
    {
        private readonly string _vaultUri;
        private readonly KeyVaultClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsClient"/> class with a client using the credentials from the key vault settings.
        /// </summary>
        /// <param name="keyVaultSettings">
        /// The <see cref="KeyVaultSettings"/> with information about the principal to use when getting secrets from a key vault.
        /// </param>
        public SecretsClient(IOptions<KeyVaultSettings> keyVaultSettings)
        {
            _vaultUri = keyVaultSettings.Value.SecretUri;
            _client = new KeyVaultClient(
                async (string authority, string resource, string scope) =>
                {
                    var authContext = new AuthenticationContext(authority);
                    var credential = new ClientCredential(
                        keyVaultSettings.Value.ClientId,
                        keyVaultSettings.Value.ClientSecret
                    );
#pragma warning disable CS0618 // Type or member is obsolete
                    // TODO: this whole thing is obsolete
                    var result = await authContext.AcquireTokenAsync(resource, credential);
#pragma warning restore CS0618 // Type or member is obsolete
                    return result.AccessToken;
                }
            );
        }

        /// <inheritdoc />
        public async Task<byte[]> GetCertificateAsync(string certificateName)
        {
            CertificateBundle cert = await _client.GetCertificateAsync(_vaultUri, certificateName);

            return cert.Cer;
        }

        /// <inheritdoc />
        public async Task<JsonWebKey> GetKeyAsync(string keyName)
        {
            KeyBundle kb = await _client.GetKeyAsync(_vaultUri, keyName);

            return kb.Key;
        }

        /// <inheritdoc />
        public KeyVaultClient GetKeyVaultClient() => _client;

        /// <inheritdoc />
        public async Task<string> GetSecretAsync(string secretName)
        {
            SecretBundle sb = await _client.GetSecretAsync(_vaultUri, secretName);

            return sb.Value;
        }

        /// <summary>
        /// Disposes the <see cref="KeyVaultClient"/> used by this instance.
        /// </summary>()
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the <see cref="KeyVaultClient"/> used by this instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _client.Dispose();
        }
    }
}
