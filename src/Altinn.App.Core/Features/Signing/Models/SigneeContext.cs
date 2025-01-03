using System.Text.Json.Serialization;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
///  Represents the context of a signee.
/// </summary>
internal sealed class SigneeContext
{
    /// <summary>
    /// The party associated with the signee state.
    /// </summary>
    public required Party Party { get; set; }

    /// <summary>The task associated with the signee state.</summary>
    [JsonPropertyName("taskId")]
    public required string TaskId { get; init; }

    /// <summary>
    /// The state of the signee.
    /// </summary>
    [JsonPropertyName("signeeState")]
    public required SigneeState SigneeState { get; set; }

    /// <summary>
    /// The organisation signee.
    /// </summary>
    [JsonPropertyName("organisationSignee")]
    public OrganisationSignee? OrganisationSignee { get; set; }

    /// <summary>
    /// The person signee.
    /// </summary>
    [JsonPropertyName("personSignee")]
    public PersonSignee? PersonSignee { get; set; }
}
