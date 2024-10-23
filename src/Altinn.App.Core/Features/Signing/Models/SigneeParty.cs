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
    public Notification? SignatureTaskReceived { get; set; }
}

/// <summary>
/// The notification setup for an event in the signature lifetime.
/// </summary>
public class Notification
{
    public Sms Sms { get; set; }
    public Email Email { get; set; }
}

/// <summary>
/// The sms notification container.
/// </summary>
public class Sms
{
    public required string MobileNumber { get; set; }
    public string? Body { get; set; }
}

/// <summary>
/// The email notification container.
/// </summary>
public class Email
{
    public string? EmailAddress { get; set; }
}
