using Altinn.App.Core.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Altinn.App.Core.Internal.App;

/// <summary>
/// Default implementation of IFrontendFeatures
/// </summary>
public class FrontendFeatures : IFrontendFeatures
{
    private readonly Dictionary<string, bool> _features = new();

    /// <summary>
    /// Default implementation of IFrontendFeatures
    /// </summary>
    public FrontendFeatures(IFeatureManager featureManager)
        : this(featureManager, hasInstanceLayoutProvider: false) { }

    /// <summary>
    /// Default implementation of IFrontendFeatures, enabling features based on the services registered by the app
    /// </summary>
    public FrontendFeatures(IFeatureManager featureManager, IServiceProviderIsService serviceProviderIsService)
        : this(featureManager, serviceProviderIsService.IsService(typeof(IInstanceLayoutProvider))) { }

    private FrontendFeatures(IFeatureManager featureManager, bool hasInstanceLayoutProvider)
    {
        _features.Add("footer", true);
        _features.Add("processActions", true);

        if (featureManager.IsEnabledAsync(FeatureFlags.JsonObjectInDataResponse).Result)
        {
            _features.Add("jsonObjectInDataResponse", true);
        }
        else
        {
            _features.Add("jsonObjectInDataResponse", false);
        }

        _features.Add("addInstanceIdentifierToLayoutRequests", hasInstanceLayoutProvider);
    }

    /// <inheritdoc />
    public Task<Dictionary<string, bool>> GetFrontendFeatures()
    {
        return Task.FromResult(_features);
    }
}
