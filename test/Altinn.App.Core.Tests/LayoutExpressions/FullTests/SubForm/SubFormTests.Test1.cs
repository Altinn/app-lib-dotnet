// Testing library/framework:
// - xUnit for test framework
// - FluentAssertions for expressive assertions
// These tests focus on LayoutEvaluator functionalities introduced/affected in the diff,
// exercising hidden-field detection and row removal behaviors for repeating groups.

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Validation;
using Altinn.App.Core.Tests.LayoutExpressions.FullTests;
using Altinn.App.Core.Tests.LayoutExpressions.TestUtilities;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.LayoutExpressions.FullTests.SubForm
{
    public class SubFormTests_Test1
    {
        private static object BuildModel(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return DynamicClassBuilder.DataObjectFromJsonDocument(doc.RootElement);
        }

        [Fact(DisplayName = "RemoveHiddenDataAsync(SetToNull): hidden repeating-group row fields are cleared to defaults")]
        public async Task RemoveHiddenDataAsync_SetToNull_HiddenRowFieldsCleared()
        {
            // Arrange: Use Test3 layout which hides a row when binding == "hideRow"
            var json = @"
            {
              ""some"": {
                ""data"": [
                  { ""binding"": ""keepRow"", ""binding2"": 2, ""binding3"": ""text"" },
                  { ""binding"": ""hideRow"", ""binding2"": 3, ""binding3"": ""text"" }
                ]
              }
            }";
            var model = BuildModel(json);
            var state = await LayoutTestUtils.GetLayoutModelTools(model, "Test3");
            var wrapper = new DataModelWrapper(model);

            // Sanity check preconditions
            wrapper.GetModelData("some.data[0].binding").Should().Be("keepRow");
            wrapper.GetModelData("some.data[1].binding").Should().Be("hideRow");
            wrapper.GetModelData("some.data[1].binding2").Should().Be(3);
            wrapper.GetModelData("some.data[1].binding3").Should().Be("text");

            // Act
            await LayoutEvaluator.RemoveHiddenDataAsync(state, RowRemovalOption.SetToNull);

            // Assert: Hidden row remains but fields are cleared/reset
            wrapper.GetModelData("some.data[0].binding").Should().Be("keepRow");
            wrapper.GetModelData("some.data[1].binding").Should().BeNull();
            wrapper.GetModelData("some.data[1].binding2").Should().Be(0);     // non-nullable numeric should reset to default
            wrapper.GetModelData("some.data[1].binding3").Should().BeNull();
        }

        [Fact(DisplayName = "RemoveHiddenDataAsync(DeleteRow): hidden repeating-group row is removed")]
        public async Task RemoveHiddenDataAsync_DeleteRow_HiddenRowRemoved()
        {
            // Arrange
            var json = @"
            {
              ""some"": {
                ""data"": [
                  { ""binding"": ""keepRow"", ""binding2"": 2, ""binding3"": ""text"" },
                  { ""binding"": ""hideRow"", ""binding2"": 3, ""binding3"": ""text"" },
                  { ""binding"": ""stay"",    ""binding2"": 4, ""binding3"": ""ok""   }
                ]
              }
            }";
            var model = BuildModel(json);
            var state = await LayoutTestUtils.GetLayoutModelTools(model, "Test3");
            var wrapper = new DataModelWrapper(model);

            // Sanity check preconditions
            wrapper.GetModelData("some.data[1].binding").Should().Be("hideRow");

            // Act
            await LayoutEvaluator.RemoveHiddenDataAsync(state, RowRemovalOption.DeleteRow);

            // Assert: Row count reduced and hidden row removed (index 1 should now be the previous index 2)
            wrapper.GetModelDataCount("some.data", System.Array.Empty<int>()).Should().Be(2);
            wrapper.GetModelData("some.data[0].binding").Should().Be("keepRow");
            wrapper.GetModelData("some.data[1].binding").Should().Be("stay");
        }

        [Fact(DisplayName = "GetHiddenFieldsForRemoval: includes fields for hidden repeating-group row")]
        public async Task GetHiddenFieldsForRemoval_IncludesHiddenRowFields()
        {
            // Arrange
            var json = @"
            {
              ""some"": {
                ""data"": [
                  { ""binding"": ""keepRow"", ""binding2"": 2, ""binding3"": ""text"" },
                  { ""binding"": ""hideRow"", ""binding2"": 3, ""binding3"": ""text"" }
                ]
              }
            }";
            var model = BuildModel(json);
            var state = await LayoutTestUtils.GetLayoutModelTools(model, "Test3");

            // Act
            var hidden = await LayoutEvaluator.GetHiddenFieldsForRemoval(state);

            // Assert: At least one hidden field belongs to the row that should be hidden (index 1)
            hidden.Should().NotBeNull();
            hidden.Should().NotBeEmpty();
            hidden.Select(h => h.Field).Any(f => f.Contains("some.data[1].")).Should().BeTrue();
        }
    }
}