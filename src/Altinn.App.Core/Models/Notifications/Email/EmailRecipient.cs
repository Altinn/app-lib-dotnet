using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Notifications.Email;

/// <summary>
/// Represents the recipient of an email.
/// </summary>
public sealed record EmailRecipient(
    [property: JsonPropertyName("emailAddress")] string? EmailAddress = null,
    [property: JsonPropertyName("organizationNumber")] string? OrganizationNumber = null,
    [property: JsonPropertyName("nationalIdentityNumber")] string? NationalIdentityNumber = null
);
