#nullable disable
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Newtonsoft.Json;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the response from an API endpoint providing a list of key-value properties.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public sealed class InstanceDto
{
    /// <summary>
    /// Gets or sets the unique id of the instance {instanceOwnerId}/{instanceGuid}.
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the instance owner information.
    /// </summary>
    [JsonProperty(PropertyName = "instanceOwner")]
    public required InstanceOwnerDto InstanceOwner { get; init; }

    /// <summary>
    /// Gets or sets the id of the application this is an instance of, e.g. {org}/{app22}.
    /// </summary>
    [JsonProperty(PropertyName = "appId")]
    public required string AppId { get; init; }

    /// <summary>
    /// Gets or sets application owner identifier, usually a abbreviation of organisation name. All in lower case.
    /// </summary>
    [JsonProperty(PropertyName = "org")]
    public required string Org { get; init; }

    /// <summary>
    /// Gets or sets a set of URLs to access the instance metadata resource.
    /// </summary>
    [JsonProperty(PropertyName = "selfLinks")]
    public required ResourceLinks SelfLinks { get; init; }

    /// <summary>
    /// Gets or sets the due date to submit the instance to application owner.
    /// </summary>
    [JsonProperty(PropertyName = "dueBefore")]
    public required DateTime? DueBefore { get; init; }

    /// <summary>
    /// Gets or sets date and time for when the instance should first become visible for the instance owner.
    /// </summary>
    [JsonProperty(PropertyName = "visibleAfter")]
    public required DateTime? VisibleAfter { get; init; }

    /// <summary>
    /// Gets or sets an object containing the instance process state.
    /// </summary>
    [JsonProperty(PropertyName = "process")]
    public required ProcessState Process { get; init; }

    /// <summary>
    /// Gets or sets the type of finished status of the instance.
    /// </summary>
    [JsonProperty(PropertyName = "status")]
    public required InstanceStatus Status { get; init; }

    /// <summary>
    /// Gets or sets a list of <see cref="CompleteConfirmation"/> elements.
    /// </summary>
    [JsonProperty(PropertyName = "completeConfirmations")]
    public required IReadOnlyList<CompleteConfirmation> CompleteConfirmations { get; init; }

    /// <summary>
    /// Gets or sets a list of data elements associated with the instance
    /// </summary>
    [JsonProperty(PropertyName = "data")]
    public required IReadOnlyList<DataElement> Data { get; init; }

    /// <summary>
    /// Gets or sets the presentation texts for the instance.
    /// </summary>
    [JsonProperty(PropertyName = "presentationTexts")]
    public required IReadOnlyDictionary<string, string> PresentationTexts { get; init; }

    /// <summary>
    /// Gets or sets the data values for the instance.
    /// </summary>
    [JsonProperty(PropertyName = "dataValues")]
    public required IReadOnlyDictionary<string, string> DataValues { get; init; }

    /// <summary>
    /// Gets or sets the date and time for when the element was created.
    /// </summary>
    [JsonProperty(PropertyName = "created")]
    public required DateTime? Created { get; init; }

    /// <summary>
    /// Gets or sets the id of the user who created this element.
    /// </summary>
    [JsonProperty(PropertyName = "createdBy")]
    public required string CreatedBy { get; init; }

    /// <summary>
    /// Gets or sets the date and time for when the element was last edited.
    /// </summary>
    [JsonProperty(PropertyName = "lastChanged")]
    public required DateTime? LastChanged { get; init; }

    /// <summary>
    /// Gets or sets the id of the user who last changed this element.
    /// </summary>
    [JsonProperty(PropertyName = "lastChangedBy")]
    public required string LastChangedBy { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    internal static InstanceDto From(Instance instance, Party instanceOwnerParty)
    {
        return new InstanceDto
        {
            Id = instance.Id,
            InstanceOwner = new InstanceOwnerDto
            {
                PartyId = instance.InstanceOwner.PartyId,
                PersonNumber = instance.InstanceOwner.PersonNumber,
                OrganisationNumber = instance.InstanceOwner.OrganisationNumber,
                Username = instance.InstanceOwner.Username,
                Party = instanceOwnerParty,
            },
            AppId = instance.AppId,
            Org = instance.Org,
            SelfLinks = instance.SelfLinks,
            DueBefore = instance.DueBefore,
            VisibleAfter = instance.VisibleAfter,
            Process = instance.Process,
            Status = instance.Status,
            CompleteConfirmations = instance.CompleteConfirmations,
            Data = instance.Data,
            DataValues = instance.DataValues,
            PresentationTexts = instance.PresentationTexts,
            Created = instance.Created,
            CreatedBy = instance.CreatedBy,
            LastChanged = instance.LastChanged,
            LastChangedBy = instance.LastChangedBy,
        };
    }
}

/// <summary>
/// Represents information to identify the owner of an instance.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class InstanceOwnerDto
{
    /// <summary>
    /// Gets or sets the party id of the instance owner (also called instance owner party id).
    /// </summary>
    [JsonProperty(PropertyName = "partyId")]
    public required string PartyId { get; init; }

    /// <summary>
    /// Gets or sets person number (national identification number) of the party. Null if the party is not a person.
    /// </summary>
    [JsonProperty(PropertyName = "personNumber")]
    public required string PersonNumber { get; init; }

    /// <summary>
    /// Gets or sets the organisation number of the party. Null if the party is not an organisation.
    /// </summary>
    [JsonProperty(PropertyName = "organisationNumber")]
    public required string OrganisationNumber { get; init; }

    /// <summary>
    /// Gets or sets the username of the party. Null if the party is not self identified.
    /// </summary>
    [JsonProperty(PropertyName = "username")]
    public required string Username { get; init; }

    /// <summary>
    /// Party information for the instance owner.
    /// </summary>
    [JsonProperty(PropertyName = "party")]
    public required Party Party { get; init; }
}
