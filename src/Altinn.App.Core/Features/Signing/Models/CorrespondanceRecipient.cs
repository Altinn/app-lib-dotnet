using System.Diagnostics.CodeAnalysis;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Models;

internal sealed class CorrespondanceRecipient
{
    internal string? OrganisationNumber { get; init; }
    internal string? SSN { get; init; }

    public CorrespondanceRecipient(Party party)
    {
        if (string.IsNullOrEmpty(OrganisationNumber) && string.IsNullOrEmpty(SSN))
        {
            throw new InvalidOperationException(
                "Signee does not have a national identification number nor an organisation number, unable to send correspondence"
            );
        }

        OrganisationNumber = party.OrgNumber;
        SSN = party.SSN;
    }

    [MemberNotNullWhen(true, nameof(SSN))]
    [MemberNotNullWhen(false, nameof(OrganisationNumber))]
    internal bool IsPerson => !string.IsNullOrEmpty(SSN);
}
