using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    public class AppListsFactory
    {
        public AppListsFactory(IEnumerable<IAppListsProvider> appListsProvider)
        {
            _appListsProviders = appListsProvider;
        }
        private IEnumerable<IAppListsProvider> _appListsProviders { get; }

        /// <summary>
        /// Finds the implementation of IAppListsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="optionsId">Id matching the options requested.</param>
        public IAppListsProvider GetOptionsProvider(string optionsId)
        {
            foreach (var appListsProvider in _appListsProviders)
            {
                if (appListsProvider.Id.ToLower() != optionsId.ToLower())
                {
                    continue;
                }

                return appListsProvider;
            }

            return new NullAppListsProvider();

        }
    }
}
