using Altinn.App.Core.Models.UserAction;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Service for handling user actions
/// </summary>
public class UserActionService: IUserActionService
{
    private readonly UserActionFactory _userActionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActionService"/> class.
    /// </summary>
    /// <param name="userActionFactory"></param>
    public UserActionService(UserActionFactory userActionFactory)
    {
        _userActionFactory = userActionFactory;
    }


    /// <inheritdoc />
    public async Task<UserActionResult> HandleAction(UserActionContext userActionContext, string actionId)
    {
        var actionHandler = _userActionFactory.GetActionHandler(actionId);
        return await actionHandler.HandleAction(userActionContext);
    }
}