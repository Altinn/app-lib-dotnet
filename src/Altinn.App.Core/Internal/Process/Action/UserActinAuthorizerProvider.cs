using Altinn.App.Core.Features;

namespace Altinn.App.Core.Internal.Process.Action;

/// <summary>
/// Register a user action authorizer for a given action and/or task
/// </summary>
public class UserActinAuthorizerProvider: IUserActinAuthorizerProvider
{
        
    /// <summary>
    /// Initializes a new instance of the <see cref="UserActinAuthorizerProvider"/> class
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="action"></param>
    /// <param name="authorizer"></param>
    public UserActinAuthorizerProvider(string? taskId, string? action, IUserActionAuthorizer authorizer)
    {
        TaskId = taskId;
        Action = action;
        Authorizer = authorizer;
    }
        
    /// <inheritdoc/>
    public string? Action { get; set; }
    
    /// <inheritdoc/>
    public string? TaskId { get; set; }
    
    /// <inheritdoc/>
    public IUserActionAuthorizer Authorizer { get; set; }
}