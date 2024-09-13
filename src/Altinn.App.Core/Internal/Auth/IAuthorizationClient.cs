using System.Security.Claims;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Auth;

/// <summary>
/// Interface for authorization functionality.
/// </summary>
public interface IAuthorizationClient
{
    /// <summary>
    /// Returns the list of parties that user has any rights for.
    /// </summary>
    Task<List<Party>?> GetPartyList(int userId);

    /// <summary>
    /// Verifies that the selected party is contained in the user's party list.
    /// </summary>
    Task<bool?> ValidateSelectedParty(int userId, int partyId);

    /// <summary>
    /// Check if the user is authorized to perform the given action on the given instance.
    /// </summary>
    Task<bool> AuthorizeAction(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        ClaimsPrincipal user,
        string action,
        string? taskId = null
    );

    /// <summary>
    /// Check if the user is authorized to perform the given actions on the given instance.
    /// </summary>
    Task<Dictionary<string, bool>> AuthorizeActions(Instance instance, ClaimsPrincipal user, List<string> actions);
}
