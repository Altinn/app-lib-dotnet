#nullable enable
using System.Runtime.Serialization;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Api.Models;

/// <summary>
/// Response object from action endpoint
/// </summary>
public class UserActionResponse
{
    /// <summary>
    /// Data models that have been updated
    /// </summary>
    public Dictionary<string, object?>? UpdatedDataModels { get; set; }
    
    /// <summary>
    /// Actions frontend should perform after action has been performed backend
    /// </summary>
    public List<string>? FrontendActions { get; set; }
    
    /// <summary>
    /// Validation issues that occured when processing action
    /// </summary>
    public Dictionary<string, List<ValidationIssue>>? ValidationIssues { get; set; }
}
