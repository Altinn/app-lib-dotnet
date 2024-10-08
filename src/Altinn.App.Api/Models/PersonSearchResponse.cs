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
}
