using Altinn.App.Core.Internal.App;
using FluentAssertions;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.App
{
    public class FrontendFeaturesTest
    {
        [Fact]
        public async Task GetFeatures_returns_list_of_enabled_features()
        {
            var featureManagerMock = new Mock<IFeatureManager>();
            IFrontendFeatures frontendFeatures = new FrontendFeatures(featureManagerMock.Object);

            var actual = await frontendFeatures.GetFrontendFeatures();

            actual.Should().Contain(new KeyValuePair<string, bool>("footer", true));
        }
    }
}
