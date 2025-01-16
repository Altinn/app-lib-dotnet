using System.Text.Json.Serialization;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

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

    /// <summary>
    /// The signature document, if it exists yet.
    /// </summary>
    /// <remarks>This is not and should not be serialized and persisted in storage, it's looked up on-the-fly when the signee contexts are retrieved through <see cref="SigningService.GetSigneeContexts"/></remarks>
    [JsonIgnore]
    public SignDocument? SignDocument { get; set; }
}
