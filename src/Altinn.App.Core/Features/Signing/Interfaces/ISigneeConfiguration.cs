namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for implementing app specific logic for deriving signees
/// </summary>
public interface ISigneeConfiguration
{
    /// <summary>
    /// This method should return a list of signees for the current siging task.
    /// </summary>
    public Task<SigneeConfigurationResult> GetSigneeConfiguration();
}

/// <summary>
/// A result containing persons and organisations that should sign and related config for each of them.
/// </summary>
public class SigneeConfigurationResult
{
    /// <summary>
    /// The signee configurations for persons that should sign.
    /// </summary>
    public required List<PersonSigneeConfig> PersonSigneeConfigs { get; set; }

    /// <summary>
    /// The signee configurations for organisations that should sign.
    /// </summary>
    public required List<OrganisationSigneeConfig> OrgansiationSigneeConfigs { get; set; }
}

/// <summary>
/// Configuration for a signee
/// </summary>
public abstract class SigneeConfig
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Notification config.
    /// </summary>
    public required NotificationConfiguration NotificationConfiguration { get; init; }
}

/// <summary>
/// Configuration for a signee
/// </summary>
public class PersonSigneeConfig : SigneeConfig
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
/// Configuration for a signee
/// </summary>
public class OrganisationSigneeConfig : SigneeConfig
{
    /// <summary>
    /// The social security number.
    /// </summary>
    public required string OrganisationNumber { get; init; }
}

/// <summary>
/// Configuration for notifications
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Should be true if the signee should be notified by email.
    /// </summary>
    public bool ShouldSendEmail { get; set; }

    /// <summary>
    /// Can be used to override what email address to send notifications to. If not set, the email address from the Altinn registry will be used.
    /// </summary>
    public string? NotificationEmailAddressOverride { get; set; }

    /// <summary>
    /// Should be true if the signee should be notified by sms.
    /// </summary>
    public bool ShouldSendSms { get; set; }

    /// <summary>
    /// Can be used to override what mobile number to send notifications to. If not set, the mobile number from the Altinn registry will be used.
    /// </summary>
    public string? NotificationMobileNumberOverride { get; set; }
}
