using Altinn.Platform.Storage.Interface.Models;
using Newtonsoft.Json;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Extension of Application model from Storage. Adds app specific attributes to the model
    /// </summary>
    public class ApplicationMetadata: Application
    {


        /// <summary>
        /// List of features and status (enabled/disabled)
        /// </summary>
        [JsonProperty(PropertyName = "features")]
        public Dictionary<string, bool>? Features { get; set; }
    }
}
