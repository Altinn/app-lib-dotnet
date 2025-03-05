using System.Diagnostics.CodeAnalysis;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class CorrespondenceRecipient
{
    internal string? OrganisationNumber { get; init; }
    internal string? SSN { get; init; }

    public CorrespondenceRecipient(Party party)
    {
        OrganisationNumber = party.OrgNumber;
        SSN = party.SSN;
        if (string.IsNullOrEmpty(OrganisationNumber) && string.IsNullOrEmpty(SSN))
        {
            throw new InvalidOperationException(
                "Signee does not have a national identification number nor an organisation number, unable to send correspondence"
            );
        }
    }

    [MemberNotNullWhen(true, nameof(SSN))]
    [MemberNotNullWhen(false, nameof(OrganisationNumber))]
    internal bool IsPerson => !string.IsNullOrEmpty(SSN);
}
