using Altinn.App.Core.Internal.App;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.App
{
    public class AppFeaturesTest
    {
        [Fact]
        public async void GetFeatures_returns_list_of_enabled_features()
        {
            Dictionary<string, bool> expected = new Dictionary<string, bool>()
            {
                { "footer", true }
            };
            IAppFeatures appFeatures = new AppFeatures();
            var actual = await appFeatures.GetEnabledFeatures();
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
