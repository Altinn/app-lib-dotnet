using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
/// Represents a person who is a signee.
/// </summary>
public class PersonSignee : ISigneeParty
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// The social security number.
    /// </summary>
    [JsonPropertyName("socialSecurityNumber")]
    public required string SocialSecurityNumber { get; init; }

    /// <summary>
    /// The last name of the signee.
    /// </summary>
    [JsonPropertyName("lastName")]
    public required string LastName { get; init; }

    /// <summary>
    /// The organisation the person signed on behalf of.
    /// </summary>
    [JsonPropertyName("onBehalfOfOrganisation")]
    public string? OnBehalfOfOrganisation { get; set; }
}

/// <summary>
/// Represents an organization that is a signee.
/// </summary>
public class OrganisationSignee : ISigneeParty
{
    /// <summary>
    /// The name of the organisation.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// The organization number.
    /// </summary>
    [JsonPropertyName("organisationNumber")]
    public required string OrganisationNumber { get; init; }
}

internal interface ISigneeParty
{
    public Notifications? Notifications { get; init; }
    public string DisplayName { get; init; }
}

/// <summary>
/// Configuration for notifications
/// </summary>
public class Notifications
{
    /// <summary>
    /// Notification for when a party has been delegated the rights to sign.
    /// </summary>
    [JsonPropertyName("onSignatureAccessRightsDelegated")]
    public Notification? OnSignatureAccessRightsDelegated { get; set; }
}

/// <summary>
/// The notification setup for an event in the signature lifetime.
/// </summary>
public class Notification
{
    /// <summary>
    /// SMS notification configuration. If not null, an SMS will be sent.
    /// </summary>
    [JsonPropertyName("sms")]
    public Sms? Sms { get; set; }

    /// <summary>
    /// Email notification configuration. If not null, an email will be sent.
    /// </summary>
    [JsonPropertyName("email")]
    public Email? Email { get; set; }
}

/// <summary>
/// The sms notification container.
/// </summary>
public class Sms
{
    /// <summary>
    /// The mobile number to send the sms to. If not set, the registry mobile number will be used.
    /// </summary>
    [JsonPropertyName("mobileNumber")]
    public string? MobileNumber { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

/// <summary>
/// The email notification container.
/// </summary>
public class Email
{
    /// <summary>
    /// The email address to send the email to. If not set, the registry email address will be used for organisations. For persons, no email will be sent.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// The subject. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
