using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    internal class NullTableAppOptionsProvider : ITableAppOptionsProvider
    {
        public string Id => string.Empty;

        public Task<AppTableOptions> GetTableAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            return Task.FromResult<AppTableOptions>(new AppTableOptions() { ListItems = null });
        }
    }
}
