using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Represents values to be used in a AppList.
    /// </summary>
    public class AppLists
    {
        public AppListsMetaData _metaData { get; set; }
        /// <summary>
        /// Gets or sets the list of options.
        /// </summary>
        public List<object> ListItems { get; set; }
    }

    public class AppListsMetaData
    {
        public int Page { get; set; }
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int TotaltItemsCount { get; set; }
        public List<string> Links { get; set; }
    }
}
