using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Altinn.App.Core.Models.Validation
{
    /// <summary>
    /// Represents a detailed message from validation.
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// The seriousness of the identified issue.
        /// </summary>
        [JsonProperty(PropertyName = "severity")]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        [JsonPropertyName("severity")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public required ValidationIssueSeverity Severity { get; set; }

        /// <summary>
        /// The unique id of the specific element with the identified issue.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        [Obsolete("Not in use", error: true)]
        public string? InstanceId { get; set; }

        /// <summary>
        /// The unique id of the data element of a given instance with the identified issue.
        /// </summary>
        [JsonProperty(PropertyName = "dataElementId")]
        [JsonPropertyName("dataElementId")]
        public string? DataElementId { get; set; }

        /// <summary>
        /// A reference to a property the issue is about.
        /// </summary>
        [JsonProperty(PropertyName = "field")]
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        /// <summary>
        /// A system readable identification of the type of issue.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// A human readable description of the issue.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// The short name of the class that crated the message (set automatically after return of list)
        /// </summary>
        /// <remarks>
        /// Intentionally not marked as "required", because it is set in <see cref="ValidationService"/>
        /// </remarks>
        [JsonProperty(PropertyName = "source")]
        [JsonPropertyName("source")]
        public string Source { get; set; } = default!;

        /// <summary>
        /// The custom text key to use for the localized text in the frontend.
        /// </summary>
        [JsonProperty(PropertyName = "customTextKey")]
        [JsonPropertyName("customTextKey")]
        public string? CustomTextKey { get; set; }

        /// <summary>
        /// The custom text key to use for the localized text in the frontend.
        /// </summary>
        [JsonProperty(PropertyName = "customTextParams")]
        [JsonPropertyName("customTextParams")]
        public List<string>? CustomTextParams { get; set; }
    }
}
