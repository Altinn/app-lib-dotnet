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
    /// Notification configuration.
    /// </summary>
    public required Notification Notification { get; init; }
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
public class Notification
{
    /// <summary>
    /// Should be true if the signee should be notified by email.
    /// </summary>
    public bool ShouldSendEmail { get; set; }

    /// <summary>
    /// Must be set for a person in order to send email notification. Can be used to override email for organisation, with fallback to registry info.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Should be true if the signee should be notified by sms.
    /// </summary>
    public bool ShouldSendSms { get; set; }

    /// <summary>
    /// Can be used to override what mobile number to send notifications to. If not set, the mobile number from the Altinn registry will be used.
    /// </summary>
    public string? MobileNumber { get; set; }
}
