using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// Represents a key value pair to be used as options in dropdown selectors.
    /// </summary>
    public class AppOption
    {
        /// <summary>
        /// The value of a given option
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; }

        /// <summary>
        /// The label of a given option
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; set; }

        /// <summary>
        /// The description of a given option
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The help text of a given option
        /// </summary>
        [JsonPropertyName("helpText")]
        public string? HelpText { get; set; }
    }
}
