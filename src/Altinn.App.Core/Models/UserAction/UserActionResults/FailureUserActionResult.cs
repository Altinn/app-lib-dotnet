namespace Altinn.App.Core.Models.UserAction.UserActionResults
{
    /// <summary>
    /// Represents a failed user action result and contains validation issues that should be displayed to the user
    /// </summary>
    public class FailureBaseUserActionResult : BaseUserActionResult
    {
        /// <summary>
        /// Validation issues that should be displayed to the user
        /// </summary>
        public ActionError? Error { get; }

        /// <summary>
        /// Creates a new instance of <see cref="error"/>
        /// </summary>
        /// <param name="clientActions"></param>
        /// <param name="clientActions"></param>
        public FailureBaseUserActionResult(ActionError error, List<ClientAction>? clientActions = null)
        {
            ClientActions = clientActions;
            Error = error;
        }
    }
}