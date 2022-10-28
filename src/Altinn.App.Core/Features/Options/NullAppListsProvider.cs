using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    internal class NullAppListsProvider : IAppListsProvider
    {
        public string Id => string.Empty;

        public Task<AppLists> GetAppListsAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            return Task.FromResult(new AppLists() { ListItems = null });
        }
    }
}
