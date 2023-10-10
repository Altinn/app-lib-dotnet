using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Interface for handling user actions
/// </summary>
public interface IUserActionService
{
    /// <summary>
    /// Handles the user action
    /// </summary>
    /// <param name="userActionContext">The context of the action</param>
    /// <param name="actionId"></param>
    /// <returns>The result of the action</returns>
    public Task<UserActionResult> HandleAction(UserActionContext userActionContext, string actionId);
}