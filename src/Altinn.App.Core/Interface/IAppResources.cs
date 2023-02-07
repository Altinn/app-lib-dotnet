using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Interface
{
    /// <summary>
    /// Interface for execution functionality
    /// </summary>
    public interface IAppResources
    {
        /// <summary>
        /// Get the app resource for the given parameters.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <param name="resource">the resource.</param>
        /// <returns>The app resource.</returns>
        byte[] GetAppResource(string org, string app, string resource);

        /// <summary>
        /// Get the app resource for the given parameters.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <param name="textResource">the resource.</param>
        /// <returns>The app resource.</returns>
        byte[] GetText(string org, string app, string textResource);

        /// <summary>
        /// Get the text resources in a specific language.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <param name="language">The two letter language code.</param>
        /// <returns>The text resources in the specified language if they exist. Otherwise null.</returns>
        Task<TextResource?> GetTexts(string org, string app, string language);

        /// <summary>
        /// Returns the model metadata for an app.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <returns>The ServiceMetadata for an app.</returns>
        [Obsolete("GetModelMetaDataJSON is no longer used by app frontend. Use GetModelJsonSchema.")]
        string GetModelMetaDataJSON(string org, string app);

        /// <summary>
        /// Returns the json schema for the provided model id.
        /// </summary>
        /// <param name="modelId">Unique identifier for the model.</param>
        /// <returns>The JSON schema for the model</returns>
        string GetModelJsonSchema(string modelId);

        /// <summary>
        /// Method that fetches the runtime resources stored in wwwroot
        /// </summary>
        /// <param name="resource">the resource</param>
        /// <returns>The filestream for the resource file</returns>
        byte[]? GetRuntimeResource(string resource);

        /// <summary>
        /// Returns the application metadata for an application.
        /// </summary>
        /// <returns>The application  metadata for an application.</returns>
        Application GetApplication();

        /// <summary>
        /// Returns the application XACML policy for an application.
        /// </summary>
        /// <returns>The application  XACML policy for an application.</returns>
        string? GetApplicationXACMLPolicy();

        /// <summary>
        /// Returns the application BPMN process for an application.
        /// </summary>
        /// <returns>The application  BPMN process for an application.</returns>
        string? GetApplicationBPMNProcess();

        /// <summary>
        /// Gets the prefill json file
        /// </summary>
        /// <param name="dataModelName">the data model name</param>
        /// <returns>The prefill json file as a string</returns>
        string? GetPrefillJson(string dataModelName = "ServiceModel");

        /// <summary>
        /// Get the class ref based on data type
        /// </summary>
        /// <param name="dataType">The datatype</param>
        /// <returns>Returns the class ref for a given datatype. An empty string is returned if no match is found.</returns>
        string GetClassRefForLogicDataType(string dataType);

        /// <summary>
        /// Gets the layouts for the app.
        /// </summary>
        /// <returns>A dictionary of FormLayout objects serialized to JSON</returns>
        string GetLayouts();

        /// <summary>
        /// Gets the the layouts settings
        /// </summary>
        /// <returns>The layout settings as a JSON string</returns>
        string? GetLayoutSettingsString();

        /// <summary>
        /// Gets the layout settings
        /// </summary>
        /// <returns>The layout settings</returns>
        LayoutSettings GetLayoutSettings();

        /// <summary>
        /// Gets the the layout sets
        /// </summary>
        /// <returns>The layout sets</returns>
        string GetLayoutSets();

        /// <summary>
        /// Gets the footer layout
        /// </summary>
        /// <returns>The footer layout</returns>
        string? GetFooter();

        /// <summary>
        /// Get the layout set definition. Return null if no layoutsets exists
        /// </summary>
        LayoutSets? GetLayoutSet();

        /// <summary>
        /// 
        /// </summary>
        LayoutSet? GetLayoutSetForTask(string taskId);

        /// <summary>
        /// Gets the layouts for av given layoutset
        /// </summary>
        /// <param name="layoutSetId">The layot set id</param>
        /// <returns>A dictionary of FormLayout objects serialized to JSON</returns>
        string GetLayoutsForSet(string layoutSetId);

        /// <summary>
        /// Gets the full layout model for the optional set
        /// </summary>
        LayoutModel GetLayoutModel(string? layoutSetId = null);

        /// <summary>
        /// Gets the the layouts settings for a layoutset
        /// </summary>
        /// <param name="layoutSetId">The layot set id</param>
        /// <returns>The layout settings as a JSON string</returns>
        string? GetLayoutSettingsStringForSet(string layoutSetId);

        /// <summary>
        /// Gets the the layouts settings for a layoutset
        /// </summary>
        /// <returns>The layout settings</returns>
        LayoutSettings? GetLayoutSettingsForSet(string? layoutSetId);

        /// <summary>
        /// Gets the ruleconfiguration for av given layoutset
        /// </summary>
        /// <returns>A dictionary of FormLayout objects serialized to JSON</returns>
        byte[] GetRuleConfigurationForSet(string id);

        /// <summary>
        /// Gets the the rule handler for a layoutset
        /// </summary>
        /// <returns>The layout settings</returns>
        byte[] GetRuleHandlerForSet(string id);
    }
}
