using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

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
    /// The organisation number of the party the user is acting on behalf of
    /// </summary>
    [JsonPropertyName("actionOnBehalfOf")]
    public OrganisationNumber? ActionOnBehalfOf { get; set; }
}
