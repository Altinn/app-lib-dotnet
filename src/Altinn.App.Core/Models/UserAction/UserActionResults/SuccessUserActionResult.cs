namespace Altinn.App.Core.Models.UserAction.UserActionResults
{
    /// <summary>
    /// Represents a successful user action result
    /// </summary>
    public class SuccessBaseUserActionResult : BaseUserActionResult
    {
        /// <summary>
        /// Gets or sets a dictionary of updated data models. Key should be elementId and value should be the updated data model
        /// </summary>
        public Dictionary<string, object>? UpdatedDataModels { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="SuccessBaseUserActionResult"/>
        /// </summary>
        /// <param name="clientActions"></param>
        public SuccessBaseUserActionResult(List<ClientAction>? clientActions = null)
        {
            ClientActions = clientActions;
        }

        /// <summary>
        /// Adds an updated data model to the result
        /// </summary>
        /// <param name="dataModelId"></param>
        /// <param name="dataModel"></param>
        public void AddUpdatedDataModel(string dataModelId, object dataModel)
        {
            UpdatedDataModels ??= new Dictionary<string, object>();
            UpdatedDataModels.Add(dataModelId, dataModel);
        }
    }
}