using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Request model for user action
/// </summary>
public class UserActionRequest
{
    /// <summary>
    /// Action performed
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }
    
    /// <summary>
    /// Additional metadata for the action
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }
}