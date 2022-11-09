using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.DataLists
{
    internal class NullDataListProvider : IDataListProvider
    {
        public string Id => string.Empty;

        public Task<DataList> GetDataListAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            return Task.FromResult(new DataList() { ListItems = null });
        }
    }
}
