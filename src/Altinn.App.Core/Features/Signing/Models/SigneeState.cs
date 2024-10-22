namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class SigneeState
{
    /// <summary>
    /// Create a new instance of the <see cref="SigneeState"/> class
    /// </summary>
    /// <param name="partyId">The identifier of the signee.</param>
    /// <param name="displayName">The display name of the signee.</param>
    /// <param name="taskId">The task associated with the signee state.</param>
    /// <param name="organisationNumber">Should contain organisation number if the signee is a organisation.</param>
    /// <param name="socialSecurityNumber">Should contain social security number if the signee is a person.</param>
    /// <param name="mobilePhone">TODO</param>
    /// <param name="email">TODO</param>
    internal SigneeState(
        string taskId,
        int partyId,
        string displayName,
        string? organisationNumber = null,
        string? socialSecurityNumber = null,
        string? mobilePhone = null,
        string? email = null
    )
    {
        TaskId = taskId;
        PartyId = partyId;
        DisplayName = displayName;
        OrganisationNumber = organisationNumber;
        SocialSecurityNumber = socialSecurityNumber;
    }

    /// <summary>The identifier of the signee.</summary>
    internal int PartyId { get; }

    /// <summary>The task associated with the signee state.</summary>
    internal string TaskId { get; set; }

    /// <summary>The display name of the signee.</summary>
    internal string DisplayName { get; }

    internal string? MobilePhone { get; }
    internal string? Email { get; }

    internal string? OrganisationNumber { get; init; }
    internal string? SocialSecurityNumber { get; init; }

    /// <summary>Indicates whether signee has been delegated rights to sign.</summary>
    internal bool IsDelegated { get; set; }

    /// <summary>Indicates whether signee has been notified to sign.</summary>
    internal bool IsNotified { get; set; }

    /// <summary>Indicates whether the signee has signed.</summary>
    internal bool HasSigned { get; set; }

    /// <summary>Indicates whether the receipt for the signature has been send to the signee.</summary>
    internal bool IsReceiptSent { get; set; }
}
