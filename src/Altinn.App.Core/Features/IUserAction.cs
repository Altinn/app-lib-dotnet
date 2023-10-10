using Altinn.App.Core.Models.UserAction;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface for implementing custom code for user actions
/// </summary>
public interface IUserAction
{
    /// <summary>
    /// The id of the user action
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Validation issues group. Used to share ownership of validation issues between actions and data validation 
    /// </summary>
    public string? ValidationGroup => null;
        
    /// <summary>
    /// Method for handling the user action
    /// </summary>
    /// <param name="context">The user action context</param>
    /// <returns>If the handling of the action was a success</returns>
    Task<UserActionResult> HandleAction(UserActionContext context);
}