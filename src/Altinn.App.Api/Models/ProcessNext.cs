using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Api.Models;

/// <summary>
/// Model for process next body
/// </summary>
public class ProcessNext
{
    /// <summary>
    /// Action performed
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// List of validation issues with <see cref="ValidationIssueSeverity.Warning" /> severity that should be ignored
    /// </summary>
    /// <remarks>
    /// If the list is null, all warnings will be ignored
    /// </remarks>
    public List<ValidationIssue>? IgnoredWarnings { get; set; }
}
