using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;
using Newtonsoft.Json;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Extension of Application model from Storage. Adds app specific attributes to the model
    /// </summary>
    public class ApplicationMetadata : Application
    {
        /// <summary>
        /// Create new instance of ApplicationMetadata
        /// </summary>
        /// <param name="id"></param>
        public ApplicationMetadata(string id)
        {
            Id = id;
        }


        /// <summary>
        /// List of features and status (enabled/disabled)
        /// </summary>
        [JsonProperty(PropertyName = "features")]
        public Dictionary<string, bool>? Features { get; set; }

        /// <summary>
        /// Configure options for handling what happens when entering the application
        /// </summary>
        [JsonProperty(PropertyName = "onEntry")]
        public new OnEntry? OnEntry { get; set; }

        /// <summary>
        /// Get AppIdentifier based on ApplicationMetadata.Id
        /// Updated by setting ApplicationMetadata.Id
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public AppIdentifier AppIdentifier => new (Id);

        /// <summary>
        /// Configure options for setting organisation logo
        /// </summary>
        [JsonProperty(PropertyName = "logo")]
        public Logo? Logo { get; set; }
    }
}
