using Newtonsoft.Json;

namespace Altinn.App.Core.Models 
{
    /// <summary>
    /// Settings for pagination
    /// </summary>
    public class Pagination 
    {
        /// <summary>
        /// A list of selectable options for amount of rows per page to show for pagination
        /// </summary>
        [JsonProperty(PropertyName = "rowsPerPageOptions")]
        public List<int>? RowsPerPageOptions { get; set; }

         /// <summary>
        /// The default selected option of rows per page to show for pagination
        /// </summary>
        [JsonProperty(PropertyName = "defaultRowsPerPage")]
        public int? DefaultRowsPerPage { get; set; }
    }
}
