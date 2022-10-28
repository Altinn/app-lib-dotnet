using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    public class AppListsService : IAppListsService
    {
        private readonly AppListsFactory _appListsFactory;

        public AppListsService(AppListsFactory appListsFactory)
        {
            _appListsFactory = appListsFactory;
        }

        public async Task<AppLists> GetAppListsAsync(string optionId, string language, Dictionary<string, string> keyValuePairs)
        {
            return await _appListsFactory.GetOptionsProvider(optionId).GetAppListsAsync(language, keyValuePairs);
        }
    }
}
