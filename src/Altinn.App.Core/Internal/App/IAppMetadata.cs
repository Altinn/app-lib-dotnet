using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.App
{
    /// <summary>
    /// Interface for fetching app metadata
    /// </summary>
    public interface IAppMetadata
    {
        /// <summary>
        /// Get Application metadata asynchronously
        /// </summary>
        /// <returns><see cref="ApplicationMetadata"/></returns>
        public Task<ApplicationMetadata?> GetApplicationMetadata();
        
        /// <summary>
        /// Returns the application XACML policy for an application.
        /// </summary>
        /// <returns>The application  XACML policy for an application.</returns>
        public Task<string?> GetApplicationXACMLPolicy();
        
        /// <summary>
        /// Returns the application BPMN process for an application.
        /// </summary>
        /// <returns>The application BPMN process.</returns>
        public Task<string?> GetApplicationBPMNProcess();
    }
}
