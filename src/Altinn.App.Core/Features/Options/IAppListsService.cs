using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    /// <summary>
    /// Interface for working with <see cref="AppLists"/>
    /// </summary>
    public interface IAppListsService
    {
        /// <summary>
        /// Get the list of options for a specific options list by its id and key/value pairs.
        /// </summary>
        /// <param name="optionId">The id of the options list to retrieve</param>
        /// <param name="language">The language code requested.</param>
        /// <param name="keyValuePairs">Optional list of key/value pairs to use for filtering and further lookup.</param>
        /// <returns>The list of options</returns>
        Task<AppLists> GetAppListsAsync(string optionId, string language, Dictionary<string, string> keyValuePairs);
    }
}
