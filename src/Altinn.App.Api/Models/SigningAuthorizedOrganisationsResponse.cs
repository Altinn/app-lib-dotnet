namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the response from the API when fetching organisations the user is authorized to sign on behalf of.
/// </summary>
public record SigningAuthorizedOrganisationsResponse
{
    /// <summary>
    /// The list of authorized organisations.
    /// </summary>
    public required List<AuthorizedOrganisationDetails> Organisations { get; init; }
}

/// <summary>
/// Represents the details of an authorized organisation.
/// </summary>
public record AuthorizedOrganisationDetails
{
    /// <summary>
    /// The organisation number.
    /// </summary>
    public required string OrgNumber { get; init; }

    /// <summary>
    /// The name of the organisation.
    /// </summary>
    public required string OrgName { get; init; }

    /// <summary>
    /// Gets or inits the ID of the party
    /// </summary>
    public required int PartyId { get; init; }
}
