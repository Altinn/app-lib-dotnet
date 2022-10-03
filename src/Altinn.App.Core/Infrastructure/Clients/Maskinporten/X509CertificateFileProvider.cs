using Altinn.ApiClients.Maskinporten.Config;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Altinn.App.Core.Infrastructure.Clients.Maskinporten
{
    /// <summary>
    /// Class for retreving a X509 sertificate from file "MaskinportenSettings--CertificatePkcs12Path" key.
    /// This is intended for use with local development.
    public class X509CertificateFileProvider : IX509CertificateProvider
    {
        private readonly MaskinportenSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="X509CertificateFileProvider"/> class.
        /// </summary>
        public X509CertificateFileProvider(IOptions<MaskinportenSettings<MaskinportenClientDefinition>> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Gets the certificate from the filename specified in MaskinportenSettings--CertificatePkcs12Path.
        /// </summary>
        public Task<X509Certificate2> GetCertificate()
        {
            string p12KeyStoreFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.CertificatePkcs12Path);
            if (p12KeyStoreFile == null)
            {
                throw new ArgumentException("Unable to fetch p12KeyStoreFile from appsettings.");
            }
            
            var signingCertificate = new X509Certificate2(
                p12KeyStoreFile,
                _settings.CertificatePkcs12Password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            return Task.FromResult(signingCertificate);
        }
    }
}
