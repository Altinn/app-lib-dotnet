using Altinn.App.Core.Features;
using Microsoft.FeatureManagement;

namespace Altinn.App.Core.Internal.App
{
    /// <summary>
    /// Default implementation of IFrontendFeatures
    /// </summary>
    public class FrontendFeatures : IFrontendFeatures
    {
        private readonly Dictionary<string, bool> features = new();
        private readonly IFeatureManager _featureManager;

        /// <summary>
        /// Default implementation of IFrontendFeatures
        /// </summary>
        public FrontendFeatures(IFeatureManager featureManager)
        {
            _featureManager = featureManager;

            features.Add("footer", true);

            if (_featureManager.IsEnabledAsync(FeatureFlags.JsonObjectInDataResponse).Result)
            {
                features.Add("json_object_in_data_response", true);
            }
        }

        /// <inheritdoc />
        public Task<Dictionary<string, bool>> GetFrontendFeatures()
        {
            return Task.FromResult(features);
        }
    }
}
