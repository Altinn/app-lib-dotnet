using Altinn.App.Core.Extensions;
using Altinn.App.Core.Models;

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
    public OrganisationNumber? OrganizationNumber { get; init; }

    /// <summary>
    /// The national identity number of the recipient.
    /// </summary>
    public NationalIdentityNumber? NationalIdentityNumber { get; init; }

    /// <summary>
    /// Boolean indicating if the recipient is reserved.
    /// </summary>
    [Obsolete(
        "This property is deprecated and will be removed in a future version. It has no effect. Reservations are deduced in the correspondence service."
    )]
    public bool IsReserved { get; init; }

    internal override void Serialise(MultipartFormDataContent content, int index) => Serialise(content);

    internal void Serialise(MultipartFormDataContent content)
    {
        AddIfNotNull(content, EmailAddress, $"Correspondence.Notification.CustomRecipient.EmailAddress");
        AddIfNotNull(content, MobileNumber, $"Correspondence.Notification.CustomRecipient.MobileNumber");
        AddIfNotNull(
            content,
            OrganizationNumber?.ToUrnFormattedString(),
            $"Correspondence.Notification.CustomRecipient.OrganizationNumber"
        );
        AddIfNotNull(
            content,
            NationalIdentityNumber?.ToUrnFormattedString(),
            $"Correspondence.Notification.CustomRecipient.NationalIdentityNumber"
        );
    }
}
