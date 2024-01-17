namespace Altinn.App.Core.Models.UserAction;

public enum ResultType
{
    Success,
    Failure,
    Redirect
}

/// <summary>
/// Represents the result of a user action
/// </summary>
public class UserActionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the user action was a success
    /// </summary>
    public ResultType ResultType { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of updated data models. Key should be dataTypeId
    /// </summary>
    public Dictionary<string, object?>? UpdatedDataModels { get; set; }

    /// <summary>
    /// Actions for the client to perform after the user action has been handled
    /// </summary>
    public List<ClientAction>? ClientActions { get; set; }

    /// <summary>
    /// Validation issues that should be displayed to the user
    /// </summary>
    public ActionError? Error { get; set; }

    /// <summary>
    /// If this is set, the client should redirect to this url
    /// </summary>
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Creates a success result
    /// </summary>
    /// <param name="clientActions"></param>
    /// <returns></returns>
    public static UserActionResult SuccessResult(List<ClientAction>? clientActions = null)
    {
        var userActionResult = new UserActionResult
        {
            ResultType = ResultType.Success,
            ClientActions = clientActions
        };
        return userActionResult;
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    /// <param name="error"></param>
    /// <param name="clientActions"></param>
    /// <returns></returns>
    public static UserActionResult FailureResult(ActionError error, List<ClientAction>? clientActions = null)
    {
        return new UserActionResult
        {
            ResultType = ResultType.Failure,
            ClientActions = clientActions,
            Error = error
        };
    }

    /// <summary>
    /// Creates a redirect result
    /// </summary>
    /// <param name="redirectUrl"></param>
    /// <returns></returns>
    public static UserActionResult RedirectResult(string redirectUrl)
    {
        return new UserActionResult
        {
            ResultType = ResultType.Redirect,
            RedirectUrl = redirectUrl
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
