namespace Altinn.App.Core.Models.UserAction;

/// <summary>
/// Defines an error object that should be returned if the action fails
/// </summary>
public class ActionError
{
    /// <summary>
    /// Machine readable error code
    /// </summary>
    public string Code { get; set; }
    
    /// <summary>
    /// Human readable error message or text key
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Error metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
}