namespace Altinn.App.Core.Models.UserAction.UserActionResults
{
    /// <summary>
    /// When this type of result is returned, the client should redirect the user to a new url.
    /// </summary>
    public class RedirectBaseUserActionResult : BaseUserActionResult
    {
        /// <summary>
        /// The url the client should redirect the user to
        /// </summary>
        public required string RedirectUrl { get; init; }

        /// <summary>
        /// Creates a new instance of <see cref="RedirectBaseUserActionResult"/>
        /// </summary>
        /// <param name="redirectUrl"></param>
        public RedirectBaseUserActionResult(string redirectUrl)
        {
            RedirectUrl = redirectUrl;
        }
    }
}