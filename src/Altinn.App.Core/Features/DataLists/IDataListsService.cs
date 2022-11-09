using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.DataLists
{
    /// <summary>
    /// Interface for working with <see cref="DataList"/>
    /// </summary>
    public interface IDataListsService
    {
        /// <summary>
        /// Get the list of options for a specific options list by its id and key/value pairs.
        /// </summary>
        /// <param name="dataListId">The id of the options list to retrieve</param>
        /// <param name="language">The language code requested.</param>
        /// <param name="keyValuePairs">Optional list of key/value pairs to use for filtering and further lookup.</param>
        /// <returns>The list of options</returns>
        Task<DataList> GetDataListAsync(string dataListId, string language, Dictionary<string, string> keyValuePairs);
    }
}
