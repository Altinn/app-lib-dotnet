#nullable disable

using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the response from the set tags API endpoint providing a list of tags and current validation issues.
/// </summary>
public class SetTagsResponse
{
    /// <summary>
    /// A list of tags represented as string values.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// List of validation issues that reported to have relevant changes after a new data element was added
    /// </summary>
    [JsonPropertyName("validationIssues")]
    public List<ValidationIssueWithSource> ValidationIssues { get; init; }
}
