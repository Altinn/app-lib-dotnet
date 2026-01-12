using System.Security.Claims;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Authorization.Platform.Authorization.Models;

namespace Altinn.App.Api.Tests.Mocks;

public class AuthorizationMock : IAuthorizationClient
{
    public Task<List<Party>?> GetPartyList(int userId)
    {
        return Task.FromResult<List<Party>?>([]);
    }

    public Task<bool?> ValidateSelectedParty(int userId, int partyId)
    {
        bool? isvalid = userId != 1;

        return Task.FromResult(isvalid);
    }

    /// <summary>
    /// Mock method that returns false for actions ending with _unauthorized, and true for all other actions.
    /// </summary>
    public async Task<bool> AuthorizeAction(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        ClaimsPrincipal user,
        string action,
        string? taskId = null
    )
    {
        await Task.CompletedTask;
        if (action.EndsWith("_unauthorized"))
        {
            return false;
        }

        return true;
    }

    public Task<Dictionary<string, bool>> AuthorizeActions(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        ClaimsPrincipal user,
        List<string> actions,
        string? taskId = null,
        string? endEvent = null
    )
    {
        Dictionary<string, bool> authorizedActions = new Dictionary<string, bool>();
        foreach (var action in actions)
        {
            if (action.EndsWith("_unauthorized"))
            {
                authorizedActions.Add(action, false);
            }
            else
            {
                authorizedActions.Add(action, true);
            }
        }

        return Task.FromResult(authorizedActions);
    }

    public Task<List<Role>> GetRoles(int userId, int partyId)
    {
        if (userId == 1 && partyId == 1)
        {
            return Task.FromResult<List<Role>>([]);
        }

        return Task.FromResult(
            new List<Role>
            {
                new() { Type = "roleType", Value = "roleValue" },
            }
        );
    }

    public Task<List<string>> GetKeyRoleOrganizationParties(int userId, List<string> orgNumbers)
    {
        throw new NotImplementedException();
    }
}
