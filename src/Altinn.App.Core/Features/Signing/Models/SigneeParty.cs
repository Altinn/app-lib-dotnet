using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
/// Represents a person who is a signee.
/// </summary>
public class SigneeParty
{
    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// The social security number.
    /// </summary>
    [JsonPropertyName("socialSecurityNumber")]
    public string? SocialSecurityNumber { get; init; }

    /// <summary>
    /// The full name of the signee. {FirstName} {LastName} or {FirstName} {MiddleName} {LastName}.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string? FullName { get; init; }

    /// <summary>
    /// The organisation the person signed on behalf of.
    /// </summary>
    [JsonPropertyName("onBehalfOfOrganisation")]
    public SigneePartyOrganisation? OnBehalfOfOrganisation { get; set; }
}

/// <summary>
///  Represents which organisation a person is signing on behalf of.
/// </summary>
public class SigneePartyOrganisation
{
    /// <summary>
    /// The name of the organisation.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The organisation number.
    /// </summary>
    [JsonPropertyName("organisationNumber")]
    public required string OrganisationNumber { get; set; }
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
    [JsonPropertyName("textResourceKey")]
    public string? TextResourceKey { get; set; }

    /// <summary>
    /// The reference used to track the sms. Can be set to a custom value. If not set, a random guid will be used.
    /// </summary>
    public string Reference { get; set; } = Guid.NewGuid().ToString();
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
    [JsonPropertyName("subjectTextResourceKey")]
    public string? SubjectTextResourceKey { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("bodyTextResourceKey")]
    public string? BodyTextResourceKey { get; set; }

    /// <summary>
    /// The reference used to track the email. Can be set to a custom value. If not set, a random guid will be used.
    /// </summary>
    public string Reference { get; set; } = Guid.NewGuid().ToString();
}
