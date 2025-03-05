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
        AddIfNotNull(
            content,
            EmailAddress,
            $"Correspondence.CustomNotificationRecipients[0].Recipients[{index}].EmailAddress"
        );
        AddIfNotNull(
            content,
            MobileNumber,
            $"Correspondence.CustomNotificationRecipients[0].Recipients[{index}].MobileNumber"
        );
        AddIfNotNull(
            content,
            OrganizationNumber,
            $"Correspondence.CustomNotificationRecipients[0].Recipients[{index}].OrganizationNumber"
        );
        AddIfNotNull(
            content,
            NationalIdentityNumber,
            $"Correspondence.CustomNotificationRecipients[0].Recipients[{index}].NationalIdentityNumber"
        );
        AddRequired(
            content,
            IsReserved.ToString(),
            $"Correspondence.CustomNotificationRecipients[0].Recipients[{index}].IsReserved"
        );
    }

    internal void Serialise(MultipartFormDataContent content, int index, int parentIndex)
    {
        AddIfNotNull(
            content,
            EmailAddress,
            $"Correspondence.CustomNotificationRecipients[{parentIndex}].Recipients[{index}].EmailAddress"
        );
        AddIfNotNull(
            content,
            MobileNumber,
            $"Correspondence.CustomNotificationRecipients[{parentIndex}].Recipients[{index}].MobileNumber"
        );
        AddIfNotNull(
            content,
            OrganizationNumber,
            $"Correspondence.CustomNotificationRecipients[{parentIndex}].Recipients[{index}].OrganizationNumber"
        );
        AddIfNotNull(
            content,
            NationalIdentityNumber,
            $"Correspondence.CustomNotificationRecipients[{parentIndex}].Recipients[{index}].NationalIdentityNumber"
        );
        AddRequired(
            content,
            IsReserved.ToString(),
            $"Correspondence.CustomNotificationRecipients[{parentIndex}].Recipients[{index}].IsReserved"
        );
    }
}
