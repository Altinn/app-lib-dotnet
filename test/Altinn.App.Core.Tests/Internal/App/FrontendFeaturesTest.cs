using Altinn.App.Core.Internal.App;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.App
{
    public class FrontendFeaturesTest
    {
        [Fact]
        public async Task GetFeatures_returns_list_of_enabled_features()
        {
            IFrontendFeatures frontendFeatures = new FrontendFeatures();
            var actual = await frontendFeatures.GetFrontendFeatures();
            actual.Should().Contain(new KeyValuePair<string, bool>("footer", true));
        }
    }
}
