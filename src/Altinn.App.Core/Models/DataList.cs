using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Represents values to be used in a DataList.
    /// </summary>
    public class DataList
    {
        /// <summary>
        /// Gets or sets the list of objects.
        /// </summary>
        public List<object> ListItems { get; set; } = new List<object>();
        /// <summary>
        /// Gets or sets the metadata of the DataList.
        /// </summary>
        public DataListMetadata _metaData { get; set; }  = new DataListMetadata();
    }

    /// <summary>
    /// Represents metadata values for a DataList.
    /// </summary>
    public class DataListMetadata
    {
        /// <summary>
        /// Gets or sets the value of the current page to support pagination.
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// Gets or sets the total number of pages to support pagination.
        /// </summary>
        public int PageCount { get; set; }
        /// <summary>
        /// Gets or sets the number of objects per page to support pagination.
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// Gets or sets the totalt number of items.
        /// </summary>
        public int TotaltItemsCount { get; set; }
        /// <summary>
        /// Gets or sets pagination links. 
        /// </summary>
        public List<string> Links { get; set; } = new List<string>();
    }
}
