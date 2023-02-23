namespace Altinn.App.Core.Internal.App
{
    public class AppFeatures : IAppFeatures
    {
        private readonly Dictionary<string, bool> features = new();

        /// <summary>
        /// Default implementation of IAppFeatures
        /// </summary>
        public AppFeatures()
        {
            features.Add("footer", true);
        }

        /// <inheritdoc />
        public Task<Dictionary<string, bool>> GetEnabledFeatures()
        {
            return Task.FromResult(features);
        }
    }
}
