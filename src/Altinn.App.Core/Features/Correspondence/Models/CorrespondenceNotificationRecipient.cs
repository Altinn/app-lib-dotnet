namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents a recipient of a correspondence notification.
/// </summary>
public sealed record CorrespondenceNotificationRecipient : MultipartCorrespondenceListItem
{
    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public string? EmailAddress { get; init; }

    /// <summary>
    /// The mobile number of the recipient.
    /// </summary>
    public string? MobileNumber { get; init; }

    /// <summary>
    /// The organization number of the recipient.
    /// </summary>
    public string? OrganizationNumber { get; init; }

    /// <summary>
    /// The national identity number of the recipient.
    /// </summary>
    public string? NationalIdentityNumber { get; init; }

    /// <summary>
    /// Boolean indicating if the recipient is reserved.
    /// </summary>
    public bool IsReserved { get; init; }

    internal override void Serialise(MultipartFormDataContent content, int index)
    {
        AddIfNotNull(content, EmailAddress, $"Correspondence.NotificationRecipients[{index}].EmailAddress");
        AddIfNotNull(content, MobileNumber, $"Correspondence.NotificationRecipients[{index}].MobileNumber");
        AddIfNotNull(content, OrganizationNumber, $"Correspondence.NotificationRecipients[{index}].OrganizationNumber");
        AddIfNotNull(
            content,
            NationalIdentityNumber,
            $"Correspondence.NotificationRecipients[{index}].NationalIdentityNumber"
        );
        AddRequired(content, IsReserved.ToString(), $"Correspondence.NotificationRecipients[{index}].IsReserved");
    }
}
