using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models;

/// <summary>
/// Altinn 3 Library option
/// </summary>
public class OrgLibraryAppOption
{
    /// <summary>
    /// Value of a given option
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Value { get; set; }

    /// <summary>
    /// Label of a given option
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; set; }

    /// <summary>
    /// Description of a given option
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Help text of a given option
    /// </summary>
    [JsonPropertyName("helpText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HelpText { get; set; }

    public required List<string> Tags { get; set; }
}
