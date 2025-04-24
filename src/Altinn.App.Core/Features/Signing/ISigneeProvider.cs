using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing;

/// <summary>
/// Interface for implementing app-specific logic for deriving signees.
/// </summary>
[ImplementableByApps]
public interface ISigneeProvider
{
    /// <summary>
    /// Used to select the correct <see cref="ISigneeProvider" /> implementation for a given signing task.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Returns a list of signees for the current signing task.
    /// </summary>
    Task<SigneeProviderResult> GetSigneesAsync(Instance instance); //TODO: Wrap parameters in a class?
}

/// <summary>
/// A result containing persons and organizations that should sign and related info for each of them.
/// </summary>
public class SigneeProviderResult
{
    /// <summary>
    /// The signees who are persons that should sign.
    /// </summary>
    public required List<ProvidedSignee> Signees { get; set; }
}

/// <summary>
/// Represents a person who is a signee.
/// </summary>
public abstract class ProvidedSignee
{
    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// Represents a signee that is a person.
    /// </summary>
    public class Person : ProvidedSignee
    {
        /// <summary>
        /// The social security number.
        /// </summary>
        [JsonPropertyName("socialSecurityNumber")]
        public required string SocialSecurityNumber { get; init; }

        /// <summary>
        /// The full name of the signee. {FirstName} {LastName} or {FirstName} {MiddleName} {LastName}.
        /// </summary>
        [JsonPropertyName("fullName")]
        public required string FullName { get; init; }
    }

    /// <summary>
    /// Represents a signee that is an organization.
    /// </summary>
    public class Organization : ProvidedSignee
    {
        /// <summary>
        /// The name of the organization.
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        /// <summary>
        /// The organization number.
        /// </summary>
        [JsonPropertyName("organizationNumber")]
        public required string OrganizationNumber { get; init; }
    }
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
    /// The body of the sms. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("bodyTextResourceKey")]
    public string? BodyTextResourceKey { get; set; }

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
    /// The email address to send the email to. If not set, the registry email address will be used for organizations. For persons, no email will be sent.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// The subject. If not set, a default will be used.
    /// </summary>
    [JsonPropertyName("subjectTextResourceKey")]
    public string? SubjectTextResourceKey { get; set; }

    /// <summary>
    /// The body. If not set, a default will be used. Replaces "$InstanceUrl" with the Url.
    /// </summary>
    [JsonPropertyName("bodyTextResourceKey")]
    public string? BodyTextResourceKey { get; set; }

    /// <summary>
    /// The reference used to track the email. Can be set to a custom value. If not set, a random guid will be used.
    /// </summary>
    public string Reference { get; set; } = Guid.NewGuid().ToString();
}
