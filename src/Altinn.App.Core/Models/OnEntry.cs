using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Models {
    
    /// <summary>
    /// The on entry configuration
    /// </summary>
    public class OnEntry : OnEntryConfig {

        /// <summary>
        /// Options for displaying the instance selection component
        /// </summary>
        [JsonPropertyName("instanceSelection")]
        public InstanceSelection? InstanceSelection { get; set; }
    }
}
