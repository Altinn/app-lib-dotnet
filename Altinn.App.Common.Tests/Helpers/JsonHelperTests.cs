using Altinn.App.Common.Helpers;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Altinn.App.Common.Tests
{
    public class JsonHelperTests
    {
        [Fact]
        public void FindChangedFields_RepeatingGroups_ShouldFindRemovedEntry()
        {
            Dictionary<string, object> changedFields = JsonHelper.FindChangedFields("{\"test\":\"to_be_removed\"}", "{}");

            changedFields.Should().HaveCount(1);
            changedFields.First(e => e.Key == "test").Value.Should().Be(null);
        }
    }
}