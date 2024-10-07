namespace Altinn.App.Api.Models;

/// <summary>
/// Represents a person
/// </summary>
public record struct PersonDto
{
    /// <summary>
    /// Gets or sets the social security number
    /// </summary>
    public string Ssn { get; init; }

    /// <summary>
    /// Gets or sets a persons name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets or sets the first name
    /// </summary>
    public string FirstName { get; init; }

    /// <summary>
    /// Gets or sets the middle name
    /// </summary>
    public string MiddleName { get; init; }

    /// <summary>
    /// Gets or sets the last name
    /// </summary>
    public string LastName { get; init; }
}
