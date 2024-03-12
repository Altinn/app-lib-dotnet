namespace Altinn.App.Core.Models.Email;
/// <summary>
/// Represents the recipient of an email.
/// </summary>
public class EmailRecipient
{
    /// <summary>
    /// The email of the recipient.
    /// </summary>
    public string EmailAddress { get; set; }
    /// <summary>
    /// The mobile number of the recipient.
    /// </summary>
    public string? MobileNumber { get; set; } // TODO: remove?
    /// <summary>
    /// The organisation number of the recipient.
    /// </summary>
    public string? OrganisationNumber { get; set; } // TODO: remove?
    /// <summary>
    /// The national identifier (NIN) of the recipient.
    /// </summary>
    public string? NationalIdentityNumber { get; set; } // TODO: remove?
    /// <summary>
    /// The recipient of an email.
    /// </summary>
    /// <param name="emailAddress">The email address of the recipient. Required.</param>
    public EmailRecipient(string emailAddress) => EmailAddress = emailAddress;
}
