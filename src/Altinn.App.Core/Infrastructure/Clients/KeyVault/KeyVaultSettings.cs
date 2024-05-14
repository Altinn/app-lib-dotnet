#nullable disable

using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AltinnCore.Authentication.Constants
{
    /// <summary>
    /// KeyVault settings
    /// </summary>
    public class KeyVaultSettings
    {
        /// <summary>
        /// The key vault reader client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The key vault client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The key vault tenant Id
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The uri to the key vault
        /// </summary>
        public string SecretUri { get; set; }

        /// <summary>
        /// Creates the client used to connect to key vault
        /// </summary>
        /// <param name="clientId">The key vault client id</param>
        /// <param name="clientSecret">The key vault client secret</param>
        public static KeyVaultClient GetClient(string clientId, string clientSecret) =>
            new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    async (string authority, string resource, string scope) =>
                    {
                        AuthenticationContext context = new AuthenticationContext(authority, TokenCache.DefaultShared);
                        ClientCredential clientCred = new ClientCredential(clientId, clientSecret);
#pragma warning disable CS0618 // Type or member is obsolete
                        // TODO: this is obsolete, fix by v9
                        AuthenticationResult authResult = await context.AcquireTokenAsync(resource, clientCred);
#pragma warning restore CS0618 // Type or member is obsolete
                        return authResult.AccessToken;
                    }
                )
            );
    }
}
