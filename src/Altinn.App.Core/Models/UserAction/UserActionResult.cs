using System.Net;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Models.UserAction;

public class UserActionResult
{
    public bool Success { get; set; }
    
    public List<string>? FieldsChanged { get; set; }
    
    public List<string>? FrontendActions { get; set; }
    
    public List<ValidationIssue>? ValidationIssues { get; set; }
    
    public HttpStatusCode StatusCode { get; set; }
    
    public static UserActionResult SuccessResult(HttpStatusCode statusCode = HttpStatusCode.OK, List<string>? fieldsChanged = null, List<string>? frontendActions = null, List<ValidationIssue>? validationIssues = null)
    {
        return new UserActionResult
        {
            Success = true,
            FieldsChanged = fieldsChanged,
            FrontendActions = frontendActions,
            StatusCode = statusCode,
            ValidationIssues = validationIssues
        };
    }
    
    public static UserActionResult FailureResult(HttpStatusCode statusCode = HttpStatusCode.InternalServerError, List<string>? frontendActions = null, List<ValidationIssue>? validationIssues = null)
    {
        return new UserActionResult
        {
            Success = false,
            FieldsChanged = null,
            FrontendActions = frontendActions,
            StatusCode = statusCode,
            ValidationIssues = validationIssues
        };
    }
}
