using System.Net;
using System.Runtime.Serialization;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Models.UserAction;

public class UserActionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the user action was a success
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Gets or sets a dictionary of updated data models. Key should be dataTypeId
    /// </summary>
    public Dictionary<string, object?>? UpdatedDataModels { get; set; } 
    
    /// <summary>
    /// Actions for the frontend to perform after the user action has been handled
    /// </summary>
    public List<string>? FrontendActions { get; set; }
    
    /// <summary>
    /// Validation issues that should be displayed to the user
    /// </summary>
    public List<ValidationIssue>? ValidationIssues { get; set; }
    
    /// <summary>
    /// Creates a success result
    /// </summary>
    /// <param name="frontendActions"></param>
    /// <param name="validationIssues"></param>
    /// <returns></returns>
    public static UserActionResult SuccessResult(List<string>? frontendActions = null, List<ValidationIssue>? validationIssues = null)
    {
        var userActionResult = new UserActionResult
        {
            Success = true,
            FrontendActions = frontendActions,
            ValidationIssues = validationIssues
        };
        return userActionResult;
    }
    
    /// <summary>
    /// Creates a failure result
    /// </summary>
    /// <param name="frontendActions"></param>
    /// <param name="validationIssues"></param>
    /// <returns></returns>
    public static UserActionResult FailureResult(List<string>? frontendActions = null, List<ValidationIssue>? validationIssues = null)
    {
        return new UserActionResult
        {
            Success = false,
            FrontendActions = frontendActions,
            ValidationIssues = validationIssues
        };
    }
    
    /// <summary>
    /// Adds an updated data model to the result
    /// </summary>
    /// <param name="dataModelId"></param>
    /// <param name="dataModel"></param>
    public void AddUpdatedDataModel(string dataModelId, object? dataModel)
    {
        if (UpdatedDataModels == null)
        {
            UpdatedDataModels = new Dictionary<string, object?>();
        }
        UpdatedDataModels.Add(dataModelId, dataModel);
    }
}
