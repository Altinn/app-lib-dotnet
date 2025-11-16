using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Tests.Common.Fixtures;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.LayoutExpressions.RemoveHiddenData;

public class OptionsWithGroup(ITestOutputHelper outputHelper)
{
    public class SkjemaModel
    {
        [JsonPropertyName("group")]
        public List<SkjemaModelRow>? Group { get; set; }
    }

    public class SkjemaModelRow
    {
        [JsonPropertyName("checked")]
        public bool Checked { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    [Fact]
    public async Task TestRemoveGroup()
    {
        var collection = new MockedServiceCollection();
        collection.OutputHelper = outputHelper;
        ;
        collection.TryAddCommonServices();
        var dataType = collection.AddDataType<SkjemaModel>(allowedContentTypes: ["application/json"]);
        collection.AddLayoutSet(
            dataType,
            """
            {
                "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/layout/layout.schema.v1.json",
                "data": {
                    "layout": [
                        {
                          "id": "MultipleSelect-LX05gH",
                          "type": "MultipleSelect",
                          "textResourceBindings": {
                            "title": "title"
                          },
                          "dataModelBindings": {
                            "group": "group",
                            "checked": "group.checked",
                            "simpleBinding": "group.value",
                            "label": "group.label"
                          },
                          "optionsId": "kommuneliste",
                          "required": true,
                          "alertOnChange": true,
                          "deletionStrategy":"soft",
                          "grid": {
                            "xs": 12,
                            "labelGrid": {
                              "xs": 12
                            },
                            "innerGrid": {
                              "md": 7
                            }
                          },
                          "hidden": true
                        }
                    ]
                }
            }
            """
        );

        await using var provider = collection.BuildServiceProvider();
        var dataMutator = await provider.CreateInstanceDataUnitOfWork(
            new SkjemaModel()
            {
                Group = [new SkjemaModelRow { Checked = true }, new SkjemaModelRow { Checked = false }],
            },
            dataType,
            null
        );

        var currentData = await dataMutator.GetFormData<SkjemaModel>();
        Assert.NotNull(currentData);
        Assert.NotNull(currentData.Group);
        Assert.NotEmpty(currentData.Group);
        var cleanData = await dataMutator.GetCleanAccessor().GetFormData<SkjemaModel>();
        Assert.NotNull(cleanData);
        Assert.Null(cleanData.Group);
    }
}
