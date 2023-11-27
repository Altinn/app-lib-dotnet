using Altinn.App.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Factory class for resolving <see cref="IAppOptionsProvider"/> implementations
    /// based on the name/id of the app options requested.
    /// </summary>
    public class AppOptionsFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppOptionsFactory"/> class.
        /// </summary>
        public AppOptionsFactory(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings)
        {
            _serviceProvider = serviceProvider;
            _appSettings = appSettings;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<AppSettings> _appSettings;

        /// <summary>
        /// Finds the implementation of IAppOptionsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="optionsId">Id matching the options requested.</param>
        public IAppOptionsProvider? GetOptionsProvider(string optionsId)
        {
            // First we check if there is a keyed service registered for the requested id.
            var keyedService = _serviceProvider.GetKeyedService<IAppOptionsProvider>(optionsId);
            if (keyedService is not null)
            {
                return keyedService;
            }


            // If no keyed service is found, we check if there is a service registered with the appropriate id.
            var allProviders = _serviceProvider.GetServices<IAppOptionsProvider>();
            foreach (var appOptionProvider in allProviders)
            {
                if (appOptionProvider.Id.Equals(optionsId, StringComparison.CurrentCultureIgnoreCase))
                {
                    return appOptionProvider;
                }
            }

            // If no service is found, we check if there is a file with the appropriate id.
            var provider = new FileAppOptionsProvider(_appSettings, optionsId);
            if (provider.FileExistForOptionId())
            {
                return provider;
            }

            return null;
        }

        /// <summary>
        /// Finds the implementation of IInstanceAppOptionsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="optionsId">Id matching the options requested.</param>
        public IInstanceAppOptionsProvider? GetInstanceOptionsProvider(string optionsId)
        {
            // Try to find a keyed service first
            var keyedProvider = _serviceProvider.GetKeyedService<IInstanceAppOptionsProvider>(optionsId);
            if (keyedProvider is not null)
            {
                return keyedProvider;
            }

            // If no keyed service is found, we check if there is a service registered with the appropriate id.
            var allProviders = _serviceProvider.GetServices<IInstanceAppOptionsProvider>();
            foreach (var instanceAppOptionProvider in allProviders)
            {
                if (instanceAppOptionProvider.Id == optionsId)
                // if (instanceAppOptionProvider.Id.Equals(optionsId, StringComparison.CurrentCultureIgnoreCase))
                {
                    return instanceAppOptionProvider;
                }
            }

            return null;
        }
    }
}
