using Altinn.Platform.Register.Models;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents a person
/// </summary>
public class PersonSearchResponse
{
    /// <summary>
    /// Gets or sets the social security number
    /// </summary>
    public required string Ssn { get; init; }

    /// <summary>
    /// Gets or sets a persons name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the first name
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or sets the middle name
    /// </summary>
    public string? MiddleName { get; init; }

    /// <summary>
    /// Gets or sets the last name
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Maps a person to a person search response
    /// </summary>
    public static PersonSearchResponse MapFromPerson(Person person)
    {
        return new PersonSearchResponse()
        {
            Ssn = person.SSN,
            Name = person.Name,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName,
        };
    }
}
