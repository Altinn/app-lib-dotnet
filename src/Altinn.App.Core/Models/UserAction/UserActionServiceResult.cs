using System.Runtime.Serialization;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Models.UserAction
{
    /// <summary>
    /// Result from the action
    /// </summary>
    public class UserActionServiceResult
    {
        /// <summary>
        /// If the action was a success
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Data models that have been updated
        /// </summary>
        public Dictionary<string, object?>? UpdatedDataModels { get; set; }
    
        /// <summary>
        /// Actions frontend should perform after action has been performed backend
        /// </summary>
        public List<string>? FrontendActions { get; set; }
    
        /// <summary>
        /// Validation issues that occured when processing action grouped by validation group
        /// </summary>
        public Dictionary<string, List<ValidationIssue>> ValidationIssues { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="UserActionServiceResult"/> with the provided parameters
        /// </summary>
        /// <param name="actionResult">Result from the action</param>
        /// <param name="validationGroup">The validation group to add the validation issues to</param>
        public UserActionServiceResult(UserActionResult actionResult, string validationGroup)
        {
            Success = actionResult.Success;
            UpdatedDataModels = actionResult.UpdatedDataModels;
            FrontendActions = actionResult.FrontendActions;
            ValidationIssues = new Dictionary<string, List<ValidationIssue>>()
            {
                { validationGroup, actionResult.ValidationIssues ?? new List<ValidationIssue>() }
            };
        }
    }
}
