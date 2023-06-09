using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Internal.Registers
{
    /// <summary>
    /// Interface for register functionality
    /// </summary>
    public interface IAltinnPartyClient
    {
        /// <summary>
        /// Returns party information
        /// </summary>
        /// <param name="partyId">The partyId</param>
        /// <returns>The party for the given partyId</returns>
        Task<Party> GetParty(int partyId);

        /// <summary>
        /// Looks up a party by person or organisation number.
        /// </summary>
        /// <param name="partyLookup">A populated lookup object with information about what to look for.</param>
        /// <returns>The party lookup containing either SSN or organisation number.</returns>
        Task<Party> LookupParty(PartyLookup partyLookup);
    }
}
