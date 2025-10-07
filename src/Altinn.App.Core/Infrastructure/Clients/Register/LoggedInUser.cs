using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Registers;

/// <summary>
/// A service for retrieving information about the logged in user from the register.
/// </summary>
public class LoggedInUser : ILoggedInUser
{
    private readonly IAltinnPartyClient _altinnPartyClient;

    /// <inheritdoc/>
    public LoggedInUser(IAltinnPartyClient altinnPartyClient)
    {
        _altinnPartyClient = altinnPartyClient;
    }

    /// <inheritdoc/>
    public async Task<string> GetCompanyNumber(int partyId)
    {
        Party party = await GetParty(partyId);

        return party.Organization.OrgNumber;
    }

    /// <inheritdoc/>
    public async Task<string> GetSSN(int partyId)
    {
        Party party = await GetParty(partyId);

        if (party.Person != null)
        {
            return party.Person.SSN;
        }
        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> IsOrganization(int partyId)
    {
        Party party = await GetParty(partyId);

        return party.Organization != null;
    }

    /// <inheritdoc/>
    public async Task<bool> IsPerson(int partyId)
    {
        Party party = await GetParty(partyId);

        return party.Person != null;
    }

    private async Task<Party> GetParty(int partyId)
    {
        return await _altinnPartyClient.GetParty(partyId);
    }
}
