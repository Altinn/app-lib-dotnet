namespace Altinn.App.Core.Internal.Registers;

/// <summary>
/// A service for retrieving information about the logged in user from the register.
/// </summary>
public interface ILoggedInUser
{
    /// <summary>
    /// Get the company number for the given partyId
    /// </summary>
    /// <param name="partyId"></param>
    /// <returns></returns>
    public Task<string> GetCompanyNumber(int partyId);

    /// <summary>
    /// Get the SSN for the given partyId
    /// </summary>
    /// <param name="partyId"></param>
    /// <returns></returns>
    public Task<string> GetSSN(int partyId);

    /// <summary>
    /// Check if the given partyId is an organization
    /// </summary>
    /// <param name="partyId"></param>
    /// <returns></returns>
    public Task<bool> IsOrganization(int partyId);

    /// <summary>
    /// Check if the given partyId is a person
    /// </summary>
    /// <param name="partyId"></param>
    /// <returns></returns>
    public Task<bool> IsPerson(int partyId);
}
