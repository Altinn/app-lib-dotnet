using System.Text;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Altinn.App.Core.Internal.App
{
    /// <summary>
    /// Default implementation of IAppMetadata
    /// </summary>
    public class AppMetadata : IAppMetadata
    {
        private readonly ILogger<AppMetadata> _logger;
        private readonly AppSettings _settings;
        private readonly IAppFeatures _appFeatures;
        private ApplicationMetadata? _application;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppMetadata"/> class.
        /// </summary>
        /// <param name="settings">The app repository settings.</param>
        /// <param name="appFeatures">Application features service</param>
        /// <param name="logger">A logger from the built in logger factory.</param>
        public AppMetadata(
            IOptions<AppSettings> settings,
            IAppFeatures appFeatures,
            ILogger<AppMetadata> logger)
        {
            _settings = settings.Value;
            _appFeatures = appFeatures;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ApplicationMetadata?> GetApplicationMetadata()
        {
            // Cache application metadata
            if (_application != null)
            {
                return _application;
            }

            string filedata = string.Empty;
            string filename = _settings.AppBasePath + _settings.ConfigurationFolder + _settings.ApplicationMetadataFileName;
            try
            {
                if (File.Exists(filename))
                {
                    filedata = await File.ReadAllTextAsync(filename, Encoding.UTF8);
                }

                _application = JsonConvert.DeserializeObject<ApplicationMetadata>(filedata)!;
                _application.Features = await _appFeatures.GetEnabledFeatures();

                return _application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong when fetching application metadata");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetApplicationXACMLPolicy()
        {
            string filename = _settings.AppBasePath + _settings.ConfigurationFolder + _settings.AuthorizationFolder + _settings.ApplicationXACMLPolicyFileName;
            try
            {
                if (File.Exists(filename))
                {
                    return await File.ReadAllTextAsync(filename, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong when fetching XACML Policy");
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<string?> GetApplicationBPMNProcess()
        {
            string filename = _settings.AppBasePath + _settings.ConfigurationFolder + _settings.ProcessFolder + _settings.ProcessFileName;
            try
            {
                if (File.Exists(filename))
                {
                    return await File.ReadAllTextAsync(filename, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong when fetching BPMNProcess");
            }

            return null;
        }
    }
}
