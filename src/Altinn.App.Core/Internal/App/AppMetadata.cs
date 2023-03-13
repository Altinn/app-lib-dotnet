using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.App
{
    /// <summary>
    /// Default implementation of IAppMetadata
    /// </summary>
    public class AppMetadata : IAppMetadata
    {
        private readonly ILogger<AppMetadata> _logger;
        private readonly AppSettings _settings;
        private readonly IFrontendFeatures _frontendFeatures;
        private ApplicationMetadata? _application;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppMetadata"/> class.
        /// </summary>
        /// <param name="settings">The app repository settings.</param>
        /// <param name="frontendFeatures">Application features service</param>
        /// <param name="logger">A logger from the built in logger factory.</param>
        public AppMetadata(
            IOptions<AppSettings> settings,
            IFrontendFeatures frontendFeatures,
            ILogger<AppMetadata> logger)
        {
            _settings = settings.Value;
            _frontendFeatures = frontendFeatures;
            _logger = logger;
        }

        /// <inheritdoc />
        /// <exception cref="System.Text.Json.JsonException">Thrown if deserialization fails</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if applicationmetadata.json file not found</exception>
        public async Task<ApplicationMetadata> GetApplicationMetadata()
        {
            // Cache application metadata
            if (_application != null)
            {
                return _application;
            }

            string filename = _settings.AppBasePath + _settings.ConfigurationFolder + _settings.ApplicationMetadataFileName;
            try
            {
                if (File.Exists(filename))
                {
                    JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    };
                    using FileStream fileStream = File.OpenRead(filename);
                    var application = await JsonSerializer.DeserializeAsync<ApplicationMetadata>(fileStream, jsonSerializerOptions);
                    if (application == null)
                    {
                        throw new JsonException($"Deserialization returned null, Could indicate problems with deserialization of {filename}");
                    }
                    application.App = application.Id.Split("/")[1];
                    application.Features = await _frontendFeatures.GetFrontendFeatures();
                    _application = application;

                    return _application;
                }

                throw new FileNotFoundException($"Unable to locate application metadata file: {filename}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Something went wrong when parsing application metadata");
                throw new JsonException("Something went wrong when parsing application metadata", ex);
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
