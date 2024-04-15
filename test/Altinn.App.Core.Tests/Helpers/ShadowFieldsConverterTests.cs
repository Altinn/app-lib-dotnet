using System.Text.Json;

using Altinn.App.Core.Helpers;

using Xunit;

namespace Altinn.App.PlatformServices.Tests.Helpers;

public class ShadowFieldsConverterTests
{
    [Fact]
    public void ShouldRemoveShadowFields_WithPrefix()
    {
        const string prefix = "AltinnSF_";
        var data = new Core.Tests.Implementation.TestData.AppDataModel.ModelWithShadowFields()
        {
            AltinnSF_hello = "hello",
            AltinnSF_test = "test",
            Property1 = 1,
            Property2 = 2,
            AltinnSF_gruppeish = new Core.Tests.Implementation.TestData.AppDataModel.AltinnSF_gruppeish()
            {
                F1 = "f1",
                F2 = "f2",
            },
            Gruppe = new List<Core.Tests.Implementation.TestData.AppDataModel.Gruppe>()
            {
                new()
                {
                    AltinnSF_gfhjelpefelt = "gfhjelpefelt",
                    Gf1 = "gf1",
                },
                new()
                {
                    AltinnSF_gfhjelpefelt = "gfhjelpefelt2",
                    Gf1 = "gf1-v2",
                }
            }
        };

        // Check that regular serialization (without modifier) includes shadow fields in result
        string serializedDataWithoutModifier = JsonSerializer.Serialize(data);
        Assert.Contains(prefix, serializedDataWithoutModifier);

        var options = JsonHelper.GetOptionsWithIgnorePrefix(prefix);

        // Check that serialization with modifier removes shadow fields from result
        string serializedData = JsonSerializer.Serialize(data, options);
        Assert.DoesNotContain(prefix, serializedData);
    }
}