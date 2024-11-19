using System.Text.Json.Serialization;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Api.Models;

/// <summary>
/// Contains the result of a person search request.
/// </summary>
public class LookupPersonResponse
{
    /// <summary>
    /// Creates a new instance of <see cref="LookupPersonResponse"/> from a person and sets the <see cref="Success"/> and <see cref="PersonDetails"/> properties accordingly.
    /// </summary>
    public static LookupPersonResponse CreateFromPerson(Person? person)
    {
        return new LookupPersonResponse
        {
            Success = person is not null,
            PersonDetails = person is not null ? PersonDetails.MapFromPerson(person) : null
        };
    }

    /// <summary>
    /// Indicates whether a person was found or not.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Contains details about the person found by the search. Null if no person was found.
    /// </summary>
    [JsonPropertyName("personDetails")]
    public PersonDetails? PersonDetails { get; init; }
}

/// <summary>
/// Contains details about a person
/// </summary>
public class PersonDetails
{
    /// <summary>
    /// The social security number
    /// </summary>
    [JsonPropertyName("ssn")]
    public required string Ssn { get; init; }

    /// <summary>
    /// The full name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The first name
    /// </summary>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    /// <summary>
    /// The middle name
    /// </summary>
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; init; }

    /// <summary>
    /// The last name
    /// </summary>
    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }

    /// <summary>
    /// Maps a person to person details
    /// </summary>
    public static PersonDetails MapFromPerson(Person person)
    {
        return new PersonDetails
        {
            Ssn = person.SSN,
            Name = person.Name,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName,
        };
    }
}
