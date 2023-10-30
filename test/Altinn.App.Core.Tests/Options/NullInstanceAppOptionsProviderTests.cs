using System;
using System.Collections.Generic;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models;
using FluentAssertions;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Options
{
    public class NullInstanceAppOptionsProviderTests
    {
        [Fact]
        public async Task Constructor_InitializedWithEmptyValues()
        {
            var provider = new NullInstanceAppOptionsProvider();

            provider.Id.Should().Be(string.Empty);
            var result = await provider.GetInstanceAppOptionsAsync(new InstanceIdentifier(12345, Guid.NewGuid()), "nb", new Dictionary<string, string>());
            result.Should().BeNull();
        }
    }
}
