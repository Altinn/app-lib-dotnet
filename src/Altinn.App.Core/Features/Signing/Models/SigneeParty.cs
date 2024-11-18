namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
/// Base class representing a signee.
/// </summary>
public abstract class SigneeParty
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Notifications configuration.
    /// </summary>
    public Notifications? Notifications { get; init; }
}

/// <summary>
/// Represents a person who is a signee.
/// </summary>
public class PersonSignee : SigneeParty
{
    /// <summary>
    /// The social security number.
    /// </summary>
    public required string SocialSecurityNumber { get; init; }

    /// <summary>
    /// The last name of the signee.
    /// </summary>
    public required string LastName { get; init; }
}

/// <summary>
/// Represents an organization that is a signee.
/// </summary>
public class OrganisationSignee : SigneeParty
{
    /// <summary>
    /// The organization number.
    /// </summary>
    public required string OrganisationNumber { get; init; }
}

/// <summary>
/// Configuration for notifications
/// </summary>
public class Notifications
{
    /// <summary>
    /// Notification for when a party has been delegated the rights to sign.
    /// </summary>
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
    public Sms? Sms { get; set; }

    /// <summary>
    /// Email notification configuration. If not null, an email will be sent.
    /// </summary>
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
    public string? MobileNumber { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used.
    /// </summary>
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
    public string? EmailAddress { get; set; }

    /// <summary>
    /// The subject. If not set, a default will be used.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used.
    /// </summary>
    public string? Body { get; set; }
}
