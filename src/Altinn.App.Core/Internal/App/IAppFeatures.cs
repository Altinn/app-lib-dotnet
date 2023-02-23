namespace Altinn.App.Core.Internal.App
{
    /// <summary>
    /// Interface reporting features and their status
    /// </summary>
    public interface IAppFeatures
    {
        /// <summary>
        /// Fetch frontend features that are supported by this backend
        /// </summary>
        /// <returns>List of frontend features enabled/disabled for this backend</returns>
        public Task<Dictionary<string, bool>> GetEnabledFeatures();
    }
}
