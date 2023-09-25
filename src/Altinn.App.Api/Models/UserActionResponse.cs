namespace Altinn.App.Api.Models;

/// <summary>
/// Response object from action endpoint
/// </summary>
public class UserActionResponse
{
    /// <summary>
    /// Fields that have changed
    /// </summary>
    public List<string>? FieldsChanged { get; set; }
    
    /// <summary>
    /// Actions frontend should perform after action has been performed backend
    /// </summary>
    public List<string>? FrontendActions { get; set; }
}
