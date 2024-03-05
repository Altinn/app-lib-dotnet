namespace Altinn.App.Core.Models.UserAction.UserActionResults;

/// <summary>
/// Represents the result of a user action
/// </summary>
public abstract class BaseUserActionResult
{
    /// <summary>
    /// Actions for the client to perform after the user action has been handled
    /// </summary>
    public List<ClientAction>? ClientActions { get; set; }
}
