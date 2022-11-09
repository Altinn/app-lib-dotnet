using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.DataLists
{
    /// <summary>
    /// Service for handling datalists.
    /// </summary>
    public class DataListsService : IDataListsService
    {
        private readonly DataListsFactory _dataListsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataListsService"/> class.
        /// </summary>
        public DataListsService(DataListsFactory dataListsFactory)
        {
            _dataListsFactory = dataListsFactory;
        }

        /// <inheritdoc/>
        public async Task<DataList> GetDataListAsync(string dataListId, string language, Dictionary<string, string> keyValuePairs)
        {
            return await _dataListsFactory.GetDataListProvider(dataListId).GetDataListAsync(language, keyValuePairs);
        }
    }
}
