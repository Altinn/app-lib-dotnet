using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Contains options for displaying the instance selection component
    /// </summary>
    public class InstanceSelection
    {
        private int? _defaultSelectedOption;

        /// <summary>
        /// A list of selectable options for amount of rows per page to show for pagination
        /// </summary>
        [JsonPropertyName("rowsPerPageOptions")]
        public List<int>? RowsPerPageOptions { get; set; }

        /// <summary>
        /// The default amount of rows per page to show for pagination
        /// </summary>
        [JsonPropertyName("defaultRowsPerPage")]
        public int? DefaultRowsPerPage { get; set; }

        /// <summary>
        /// The default selected option for rows per page to show for pagination
        /// </summary>
        [JsonPropertyName("defaultSelectedOption")]
        public int? DefaultSelectedOption
        {
            get { return _defaultSelectedOption ?? DefaultRowsPerPage; }
            set { _defaultSelectedOption = value; }
        }

        /// <summary>
        /// The direction of sorting the list of instances, asc or desc
        /// </summary>
        [JsonPropertyName("sortDirection")]
        public string? SortDirection { get; set; }
    }
}
