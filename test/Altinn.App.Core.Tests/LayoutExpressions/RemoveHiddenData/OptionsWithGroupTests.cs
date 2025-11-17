using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Tests.Common.Fixtures;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.LayoutExpressions.RemoveHiddenData;

public class OptionsWithGroupTests
{
    private readonly MockedServiceCollection _collection;
    private readonly DataType _dataType;

    public class SkjemaModel
    {
        [JsonPropertyName("group")]
        public List<SkjemaModelRow>? Group { get; set; }

        [JsonPropertyName("hideGroup")]
        public bool HideGroup { get; set; }
    }

    public class SkjemaModelRow
    {
        [JsonPropertyName("checked")]
        public bool Checked { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("unmapped")]
        public string? Unmapped { get; set; }
    }

    public OptionsWithGroupTests(ITestOutputHelper outputHelper)
    {
        _collection = new MockedServiceCollection();
        _collection.OutputHelper = outputHelper;
        _collection.TryAddCommonServices();
        _dataType = _collection.AddDataType<SkjemaModel>();

        _collection.AddLayoutSet(
            _dataType,
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
                          "deletionStrategy":"hard",
                          "grid": {
                            "xs": 12,
                            "labelGrid": {
                              "xs": 12
                            },
                            "innerGrid": {
                              "md": 7
                            }
                          },
                          "hidden": ["dataModel", "hideGroup"]
                        }
                    ]
                }
            }
            """
        );
    }

    [Fact]
    public async Task TestRemoveGroup()
    {
        await using var provider = _collection.BuildServiceProvider();
        var dataMutator = await provider.CreateInstanceDataUnitOfWork(
            new SkjemaModel()
            {
                Group = [new SkjemaModelRow { Checked = true }, new SkjemaModelRow { Checked = false }],
                HideGroup = true,
            },
            _dataType,
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

    [Fact]
    public async Task TestRemoveUnchecked()
    {
        await using var provider = _collection.BuildServiceProvider();
        var dataMutator = await provider.CreateInstanceDataUnitOfWork(
            new SkjemaModel()
            {
                Group =
                [
                    new SkjemaModelRow
                    {
                        Checked = true,
                        Label = "label1",
                        Value = "value1",
                        Unmapped = "unmapped1",
                    },
                    new SkjemaModelRow
                    {
                        Checked = false,
                        Label = "label2",
                        Value = "value2",
                        Unmapped = "unmapped2",
                    },
                ],
                HideGroup = false,
            },
            _dataType,
            null
        );

        var currentData = await dataMutator.GetFormData<SkjemaModel>();
        Assert.NotNull(currentData);
        Assert.NotNull(currentData.Group);
        Assert.NotEmpty(currentData.Group);
        var cleanData = await dataMutator.GetCleanAccessor().GetFormData<SkjemaModel>();
        Assert.NotNull(cleanData);
        Assert.NotNull(cleanData.Group);
        Assert.NotNull(cleanData.Group[0]);
        Assert.NotSame(currentData.Group[0], cleanData.Group[0]);
        Assert.Equal("label1", cleanData.Group[0].Label);
        Assert.Equal("value1", cleanData.Group[0].Value);
        Assert.Equal("unmapped1", cleanData.Group[0].Unmapped);
        Assert.Null(cleanData.Group[1]);
    }
}
