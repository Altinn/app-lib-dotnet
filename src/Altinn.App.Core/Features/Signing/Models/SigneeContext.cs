using System.Text.Json.Serialization;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
///  Represents the context of a signee.
/// </summary>
public sealed class SigneeContext
{
    /// <summary>The task associated with the signee state.</summary>
    [JsonPropertyName("taskId")]
    public required string TaskId { get; init; }

    /// <summary>
    /// The original party associated with the signee.
    /// </summary>
    [JsonPropertyName("party")]
    public required Party Party { get; set; }

    /// <summary>
    /// The social security number.
    /// </summary>
    [JsonPropertyName("socialSecurityNumber")]
    public string? SocialSecurityNumber { get; set; }

    /// <summary>
    /// The full name of the signee. {FirstName} {LastName} or {FirstName} {MiddleName} {LastName}.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    /// <summary>
    /// The organisation the person signed on behalf of.
    /// </summary>
    [JsonPropertyName("onBehalfOfOrganisation")]
    public SigneeContextOrganisation? OnBehalfOfOrganisation { get; set; }

    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// The state of the signee.
    /// </summary>
    [JsonPropertyName("signeeState")]
    public required SigneeState SigneeState { get; set; }

    /// <summary>
    /// The signature document, if it exists yet.
    /// </summary>
    /// <remarks>This is not and should not be serialized and persisted in storage, it's looked up on-the-fly when the signee contexts are retrieved through <see cref="SigningService.GetSigneeContexts"/></remarks>
    [JsonIgnore]
    public SignDocument? SignDocument { get; set; }
}

/// <summary>
/// Represents what organisation a person is signing on behalf of.
/// </summary>
public class SigneeContextOrganisation
{
    /// <summary>
    /// The name of the organisation.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The organisation number.
    /// </summary>
    [JsonPropertyName("organisationNumber")]
    public required string OrganisationNumber { get; set; }
}
