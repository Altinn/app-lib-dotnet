using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Microsoft.Extensions.Options;

//TODO: Test Eformidling integration point in test environment

namespace Altinn.App.Core.Infrastructure.Clients.Maskinporten
{
    /// <summary>
    /// Identifies the client to be authorized by Maskinporten.
    /// </summary>
    public class MaskinportenClientDefinition : IClientDefinition
    {
        private readonly IX509CertificateProvider _certificateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskinportenClientDefinition"/> class.
        /// </summary>
        public MaskinportenClientDefinition(IX509CertificateProvider certificateProvider, IOptions<MaskinportenSettings<MaskinportenClientDefinition>> clientSettings)
        {
            _certificateProvider = certificateProvider;
            ClientSettings = clientSettings.Value;
        }

        /// <inheritDoc/>
        public MaskinportenSettings ClientSettings { get; set; }

        /// <summary>
        /// Gets the <see cref="ClientSecrets"/>
        /// </summary>
        public async Task<ClientSecrets> GetClientSecrets()
        {
            var clientSecrets = new ClientSecrets()
            {
                ClientCertificate = await _certificateProvider.GetCertificate()
            };
            
            return clientSecrets;
        }
    }
}
