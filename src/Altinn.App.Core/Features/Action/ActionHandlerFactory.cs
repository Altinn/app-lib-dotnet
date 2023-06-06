namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Factory class for resolving <see cref="IActionHandler"/> implementations
/// based on the id of the action.
/// </summary>
public class ActionHandlerFactory
{
    private readonly IEnumerable<IActionHandler> _actionHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionHandlerFactory"/> class.
    /// </summary>
    /// <param name="actionHandlers">The list of action handlers to choose from.</param>
    public ActionHandlerFactory(IEnumerable<IActionHandler> actionHandlers)
    {
        _actionHandlers = actionHandlers;
    }

    /// <summary>
    /// Find the implementation of <see cref="IActionHandler"/> based on the actionId
    /// </summary>
    /// <param name="actionId">The id of the action to handle.</param>
    /// <returns>The first implementation of <see cref="IActionHandler"/> that matches the actionId. If no match <see cref="NullActionHandler"/> is returned</returns>
    public IActionHandler GetActionHandler(string? actionId)
    {
        if (actionId != null)
        {
            foreach (var actionHandler in _actionHandlers)
            {
                if (actionHandler.Id.ToLower().Equals(actionId.ToLower()))
                {
                    return actionHandler;
                }
            }
        }

        return new NullActionHandler();
    }
}