using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models
{
    /// <summary>
    /// The Logo configuration
    /// </summary>
    public class Logo
    {
        /// <summary>
        /// A flag to specify that the form should display appOwner in header
        /// </summary>
        [JsonPropertyName("displayAppOwnerNameInHeader")]
        public bool DisplayAppOwnerNameInHeader { get; set; }

        /// <summary>
        /// Specifies from where the logo url should be fetched
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Specifies the size of the logo. Can have the values
        /// 'small', 'medium', or 'large'
        /// </summary>
        [JsonPropertyName("size")]
        public string Size { get; set; } = "small";
    }
}
