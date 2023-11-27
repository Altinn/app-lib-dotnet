using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Service for handling app options aka code lists.
    /// </summary>
    public class AppOptionsService : IAppOptionsService
    {
        private readonly AppOptionsFactory _appOpptionsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppOptionsService"/> class.
        /// </summary>
        public AppOptionsService(AppOptionsFactory appOptionsFactory)
        {
            _appOpptionsFactory = appOptionsFactory;
        }

        /// <inheritdoc/>
        public async Task<AppOptions?> GetOptionsAsync(string optionId, string language, Dictionary<string, string> keyValuePairs)
        {
            var proivder = _appOpptionsFactory.GetOptionsProvider(optionId);
            if (proivder is not null)
            {
                return await proivder.GetAppOptionsAsync(language, keyValuePairs);
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<AppOptions?> GetOptionsAsync(InstanceIdentifier instanceIdentifier, string optionId, string language, Dictionary<string, string> keyValuePairs)
        {
            var provider = _appOpptionsFactory.GetInstanceOptionsProvider(optionId);
            if (provider is not null)
            {
                return await provider.GetInstanceAppOptionsAsync(instanceIdentifier, language, keyValuePairs);
            }

            return null;
        }
    }
}