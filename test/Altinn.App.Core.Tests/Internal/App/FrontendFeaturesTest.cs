#nullable disable
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.App.Core.Tests.Internal.App;

public class FrontendFeaturesTest
{
    [Fact]
    public async Task GetFeatures_returns_list_of_enabled_features()
    {
        Dictionary<string, bool> expected = new Dictionary<string, bool>()
        {
            { "footer", true },
            { "processActions", true },
            { "jsonObjectInDataResponse", false },
            { "addInstanceIdentifierToLayoutRequests", false },
        };
        var featureManagerMock = new Mock<IFeatureManager>();
        IFrontendFeatures frontendFeatures = new FrontendFeatures(featureManagerMock.Object);

        var actual = await frontendFeatures.GetFrontendFeatures();

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetFeatures_returns_list_of_enabled_features_when_feature_flag_is_enabled()
    {
        Dictionary<string, bool> expected = new Dictionary<string, bool>()
        {
            { "footer", true },
            { "processActions", true },
            { "jsonObjectInDataResponse", true },
            { "addInstanceIdentifierToLayoutRequests", false },
        };
        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.JsonObjectInDataResponse)).ReturnsAsync(true);
        IFrontendFeatures frontendFeatures = new FrontendFeatures(featureManagerMock.Object);
        var actual = await frontendFeatures.GetFrontendFeatures();
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetFeatures_enables_instance_layout_requests_when_app_has_instance_layout_provider(
        bool registerProvider
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IFeatureManager>().Object);
        services.AddSingleton<IFrontendFeatures, FrontendFeatures>();
        if (registerProvider)
        {
            services.AddTransient<IInstanceLayoutProvider, InstanceLayoutProvider>();
        }
        await using var serviceProvider = services.BuildServiceProvider();
        var frontendFeatures = serviceProvider.GetRequiredService<IFrontendFeatures>();

        var actual = await frontendFeatures.GetFrontendFeatures();

        Assert.Equal(registerProvider, actual["addInstanceIdentifierToLayoutRequests"]);
    }

    private sealed class InstanceLayoutProvider : IInstanceLayoutProvider
    {
        public Task<string> GetLayouts(InstanceLayoutContext context) => Task.FromResult<string>(null);

        public Task<string> GetLayoutSettings(InstanceLayoutContext context) => Task.FromResult<string>(null);
    }
}
