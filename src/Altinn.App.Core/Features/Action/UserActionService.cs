using Altinn.App.Core.Internal;
using Altinn.App.Core.Internal.Exceptions;
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
    /// <exception cref="NotFoundException">Thrown if no Handler for the actionId is found</exception>
    public async Task<UserActionServiceResult> HandleAction(UserActionContext userActionContext, string actionId)
    {
        var actionHandler = _userActionFactory.GetActionHandlerOrDefault(actionId);
        if (actionHandler == null)
        {
            throw new NotFoundException($"Action handler for action {actionId} not found");
        }
        string validationGroup = actionHandler.ValidationGroup ?? actionHandler.GetType().ToString();
        return new UserActionServiceResult(await actionHandler.HandleAction(userActionContext), validationGroup);
    }
}