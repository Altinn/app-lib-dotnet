using Newtonsoft.Json;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Contains options for displaying the instance selection component
    /// </summary>
    public class InstanceSelection
    {
        /// <summary>
        /// The direction of sorting the list of instances, asc or desc
        /// </summary>
        [JsonProperty(PropertyName = "sortDirection")]
        public string? SortDirection { get; set; }

        /// <summary>
        /// Settings for pagination
        /// </summary>
        [JsonProperty(PropertyName = "pagination")]
        public Pagination? Pagination { get; set; } 
    }
}
