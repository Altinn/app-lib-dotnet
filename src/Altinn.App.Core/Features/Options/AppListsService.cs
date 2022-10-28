using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Service for handling app lists.
    /// </summary>
    public class AppListsService : IAppListsService
    {
        private readonly AppListsFactory _appListsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppListsService"/> class.
        /// </summary>
        public AppListsService(AppListsFactory appListsFactory)
        {
            _appListsFactory = appListsFactory;
        }

        /// <inheritdoc/>
        public async Task<AppLists> GetAppListsAsync(string optionId, string language, Dictionary<string, string> keyValuePairs)
        {
            return await _appListsFactory.GetAppListsProvider(optionId).GetAppListsAsync(language, keyValuePairs);
        }
    }
}
