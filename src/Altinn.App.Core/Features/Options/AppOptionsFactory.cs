namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Factory class for resolving <see cref="IAppOptionsProvider"/> implementations
    /// based on the name/id of the app options requested.
    /// </summary>
    public class AppOptionsFactory
    {
        private const string DEFAULT_PROVIDER_NAME = "default";

        /// <summary>
        /// Initializes a new instance of the <see cref="AppOptionsFactory"/> class.
        /// </summary>
        public AppOptionsFactory(IEnumerable<IAppOptionsProvider> appOptionsProviders)
        {
            _appOptionsProviders = appOptionsProviders;
        }

        private IEnumerable<IAppOptionsProvider> _appOptionsProviders { get; }

        /// <summary>
        /// Finds the implementation of IAppOptionsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="optionsId">Id matching the options requested.</param>
        public IAppOptionsProvider GetOptionsProvider(string optionsId)
        {
            bool isDefault = optionsId == DEFAULT_PROVIDER_NAME;

            foreach (var appOptionProvider in _appOptionsProviders)
            {
                if (!appOptionProvider.Id.Equals(optionsId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return appOptionProvider;
            }

            if (isDefault)
            {
                throw new KeyNotFoundException(
                    "No default app options provider found in the configures services. Please check your services configuration."
                );
            }

            // In the case of no providers registred specifically for the requested id,
            // we use the default provider as base. Hence we set the requested id as this is
            // the key for finding the options file.
            var defaultAppOptions = (DefaultAppOptionsProvider)GetOptionsProvider(DEFAULT_PROVIDER_NAME);
            var clonedAppOptions = defaultAppOptions.CloneDefaultTo(optionsId);

            return clonedAppOptions;
        }
    }
}
