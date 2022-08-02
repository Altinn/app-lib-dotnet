using Altinn.App.Common.Helpers;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Altinn.App.Common.Tests.Helpers.JsonHelperTests
{
    public class JsonHelperTests
    {
        [Fact]
        public async void FindChangedFields_RepeatingGroups_ShouldFindRemovedEntry()
        {
            var before = await EmbeddedResource.LoadDataAsString(
                "Altinn.App.Common.Tests.Helpers.JsonHelperTests.TestData.before.json"
            );
            var after = await EmbeddedResource.LoadDataAsString(
                "Altinn.App.Common.Tests.Helpers.JsonHelperTests.TestData.after.json"
            );

            Dictionary<string, object> changedFields = JsonHelper.FindChangedFields(before, after);

            changedFields.Should().HaveCount(18);
            changedFields.Should().Equal(new Dictionary<string, object>
            {
                {"willBeRemoved", null},
                {"willChangeValue", false},
                {"object.willBeRemoved", null},
                {"object.listInObject[2]", null},

                // One item has been removed, so the later values have been shifted up
                {"moreAdvanced.oneRemovedInList[2]", "kept3"},
                {"moreAdvanced.oneRemovedInList[3]", "kept4"},
                {"moreAdvanced.oneRemovedInList[4]", null},

                {"moreAdvanced.objectWithRemovedProperty.removed1", null},

                {"moreAdvanced.objectWithRemovedPropertyAndInnerChanges.removed1", null},
                {"moreAdvanced.objectWithRemovedPropertyAndInnerChanges.removed2", null},
                {"moreAdvanced.objectWithRemovedPropertyAndInnerChanges.kept1.removedProp", null},
                {"moreAdvanced.objectWithRemovedPropertyAndInnerChanges.kept2.second", true},

                {"moreAdvanced.mixedList[0].first", false},
                {"moreAdvanced.mixedList[1].first", true},
                {"moreAdvanced.mixedList[2]", "some string that changes"},

                // This index used to be a number, but the number was removed, so what was [4] is now [3].
                // We need to tell the client that the scalar value in [3] is no more, while also putting
                // an object there to replace it.
                // {"moreAdvanced.mixedList[3]", null}, // TODO: Implement
                {"moreAdvanced.mixedList[3].first", "absolutely not"},

                {"moreAdvanced.mixedList[4].first", null},
                {"moreAdvanced.mixedList[4].otherRemovedProp", null},
            });
        }
    }
}