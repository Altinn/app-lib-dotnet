using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Factory class for resolving <see cref="IAppListsProvider"/> implementations
    /// based on the name/id of the app lists requested.
    /// </summary>
    public class AppListsFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppListsFactory"/> class.
        /// </summary>
        public AppListsFactory(IEnumerable<IAppListsProvider> appListsProvider)
        {
            AppListsProviders = appListsProvider;
        }
        private IEnumerable<IAppListsProvider> AppListsProviders { get; }

        /// <summary>
        /// Finds the implementation of IAppListsProvider based on the options id
        /// provided.
        /// </summary>
        /// <param name="listId">Id matching the options requested.</param>
        public IAppListsProvider GetAppListsProvider(string listId)
        {
            foreach (var appListsProvider in AppListsProviders)
            {
                if (appListsProvider.Id.ToLower().Equals(listId.ToLower()))
                {
                    return appListsProvider;
                }
            }

            return new NullAppListsProvider();

        }
    }
}
