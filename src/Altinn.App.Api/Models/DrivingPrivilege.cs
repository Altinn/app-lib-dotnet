using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents a driving privilege from a driver's license credential.
/// </summary>
public class DrivingPrivilege
{
    /// <summary>
    /// The vehicle category code (e.g., "B", "C", "D").
    /// </summary>
    [JsonPropertyName("vehicle_category_code")]
    public string? VehicleCategoryCode { get; init; }

    /// <summary>
    /// The date the privilege was issued (YYYY-MM-DD format).
    /// </summary>
    [JsonPropertyName("issue_date")]
    public string? IssueDate { get; init; }

    /// <summary>
    /// The date the privilege expires (YYYY-MM-DD format).
    /// </summary>
    [JsonPropertyName("expiry_date")]
    public string? ExpiryDate { get; init; }

    [JsonPropertyName("codes")]
    public string? Codes { get; init; }
}
