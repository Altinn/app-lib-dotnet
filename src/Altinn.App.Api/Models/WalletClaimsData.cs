using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Claims data from a verified wallet credential (e.g., mobile driver's license).
/// </summary>
public class WalletClaimsData
{
    /// <summary>
    /// Base64-encoded portrait image (JPEG format).
    /// </summary>
    [JsonPropertyName("portrait")]
    public string? Portrait { get; init; }

    /// <summary>
    /// The person's given name(s).
    /// </summary>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }

    /// <summary>
    /// The person's family name.
    /// </summary>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }

    /// <summary>
    /// The person's date of birth (YYYY-MM-DD format).
    /// </summary>
    [JsonPropertyName("birth_date")]
    public string? BirthDate { get; init; }

    /// <summary>
    /// The date the credential was issued (YYYY-MM-DD format).
    /// </summary>
    [JsonPropertyName("issue_date")]
    public string? IssueDate { get; init; }

    /// <summary>
    /// The date the credential expires (YYYY-MM-DD format).
    /// </summary>
    [JsonPropertyName("expiry_date")]
    public string? ExpiryDate { get; init; }

    /// <summary>
    /// The issuing country (ISO 3166-1 alpha-2 code, e.g., "NO").
    /// </summary>
    [JsonPropertyName("issuing_country")]
    public string? IssuingCountry { get; init; }

    /// <summary>
    /// The credential/document number.
    /// </summary>
    [JsonPropertyName("document_number")]
    public int? DocumentNumber { get; init; }

    /// <summary>
    /// List of driving privileges (for driver's license credentials).
    /// </summary>
    [JsonPropertyName("driving_privileges")]
    public DrivingPrivilege? DrivingPrivileges { get; init; }
}
