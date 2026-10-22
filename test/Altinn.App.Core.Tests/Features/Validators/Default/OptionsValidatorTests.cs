using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Features.Options.Altinn3LibraryCodeList;
using Altinn.App.Core.Features.Validation.Default;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Validation;
using Altinn.App.Core.Tests.LayoutExpressions.TestUtilities;
using Altinn.App.Tests.Common;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.Features.Validators.Default;

public class OptionsValidatorTests
{
    private const string DataElementId = "fc9e8d8c-4b9f-4f59-9a3e-6a1c3e4b5d6f";
    private const string InlineOptions = """[{ "value": "a", "label": "A" }, { "value": "b", "label": "B" }]""";
    private const string OptionsIdComponent =
        """{ "id": "dropdown", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "value" }, "optionsId": "list" }""";

    private readonly ITestOutputHelper _output;
    private readonly Mock<IAppResources> _appResources = new();
    private readonly Mock<IAppOptionsService> _appOptionsService = new();
    private readonly Mock<IAltinn3LibraryCodeListService> _altinn3LibraryCodeListService = new();
    private readonly OptionsValidator _validator;

    public OptionsValidatorTests(ITestOutputHelper output)
    {
        _output = output;
        _validator = new OptionsValidator(
            _appResources.Object,
            _appOptionsService.Object,
            _altinn3LibraryCodeListService.Object,
            new TranslationService(
                new AppIdentifier("org", "app"),
                _appResources.Object,
                FakeLoggerXunit.Get<TranslationService>(output)
            ),
            FakeLoggerXunit.Get<OptionsValidator>(output)
        );
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("b", false)]
    [InlineData("x", true)]
    [InlineData("A", true)]
    [InlineData("a,b", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task Validate_ReturnsIssue_WhenValueIsNotAnOption(string? value, bool expectIssue)
    {
        var issues = await Validate(
            $$"""[{ "id": "radio", "type": "RadioButtons", "dataModelBindings": { "simpleBinding": "value" }, "options": {{InlineOptions}} }]""",
            new OptionsTestModel { Value = value }
        );

        if (expectIssue)
        {
            var issue = Assert.Single(issues);
            Assert.Equal("value", issue.Field);
            Assert.Equal(DataElementId, issue.DataElementId);
            Assert.Equal("InvalidOption", issue.Code);
            Assert.Equal("Verdien er ikke et av alternativene", issue.Description);
            Assert.Null(issue.CustomTextKey);
            Assert.Equal("radio", issue.CustomTextParameters?["componentId"]);
            Assert.Equal(value, issue.CustomTextParameters?["invalidValues"]);
            Assert.Equal(ValidationIssueSeverity.Error, issue.Severity);
        }
        else
        {
            Assert.Empty(issues);
        }
    }

    [Theory]
    [InlineData("Checkboxes", "a,b", false)]
    [InlineData("Checkboxes", "b", false)]
    [InlineData("Checkboxes", "a,x", true)]
    [InlineData("Checkboxes", "a, b", true)]
    [InlineData("MultipleSelect", "a,b", false)]
    [InlineData("MultipleSelect", "x", true)]
    [InlineData("Dropdown", "a,b", true)]
    public async Task Validate_ChecksEachValue_OnlyForMultiSelect(string type, string value, bool expectIssue)
    {
        var issues = await Validate(
            $$"""[{ "id": "multi", "type": "{{type}}", "dataModelBindings": { "simpleBinding": "value" }, "options": {{InlineOptions}} }]""",
            new OptionsTestModel { Value = value }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Theory]
    [InlineData("\"hidden\": true", false)]
    [InlineData("\"hidden\": [\"equals\", [\"dataModel\", \"value\"], \"x\"]", false)]
    [InlineData("\"readOnly\": true", false)]
    [InlineData("\"readOnly\": [\"concat\", \"tr\", \"ue\"]", false)]
    [InlineData("\"renderAsSummary\": true", false)]
    [InlineData("\"readOnly\": false", true)]
    [InlineData("\"readOnly\": [\"dataModel\", \"note\"]", true)]
    [InlineData("\"renderAsSummary\": \"yes\"", true)]
    [InlineData("\"readOnly\": [\"authContext\", \"write\"]", false)]
    [InlineData("\"readOnly\": [\"dataModel\", \"value\"]", true)]
    public async Task Validate_SkipsComponent_OnlyWhenHiddenOrNotEditable(string property, bool expectIssue)
    {
        var issues = await Validate(
            $$"""[{ "id": "radio", "type": "RadioButtons", {{property}}, "dataModelBindings": { "simpleBinding": "value" }, "options": {{InlineOptions}} }]""",
            new OptionsTestModel { Value = "x" }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Theory]
    [InlineData("""{ "group": "rows", "simpleBinding": "rows.id" }""")]
    [InlineData("{}")]
    public async Task Validate_ReturnsNoIssues_WhenSelectionIsStoredInGroupOrNotBound(string dataModelBindings)
    {
        var issues = await Validate(
            $$"""[{ "id": "checkboxes", "type": "Checkboxes", "dataModelBindings": {{dataModelBindings}}, "options": {{InlineOptions}} }]""",
            new OptionsTestModel { Rows = [new OptionsTestRow { Id = "x" }] }
        );

        Assert.Empty(issues);
    }

    [Fact]
    public async Task Validate_ReturnsIssueWithRowIndex_WhenValueInRepeatingGroupIsNotAnOption()
    {
        var issues = await Validate(
            $$"""
            [
              { "id": "group", "type": "RepeatingGroup", "dataModelBindings": { "group": "rows" }, "children": ["answer"] },
              { "id": "answer", "type": "RadioButtons", "dataModelBindings": { "simpleBinding": "rows.answer" }, "options": {{InlineOptions}} }
            ]
            """,
            new OptionsTestModel { Rows = [new OptionsTestRow { Answer = "a" }, new OptionsTestRow { Answer = "x" }] }
        );

        var issue = Assert.Single(issues);
        Assert.Equal("rows[1].answer", issue.Field);
    }

    [Theory]
    [InlineData("number", """[{ "value": 1.5, "label": "1.5" }, { "value": 2, "label": "2" }]""", false)]
    [InlineData("flag", """[{ "value": true, "label": "Yes" }, { "value": false, "label": "No" }]""", false)]
    [InlineData("value", """[{ "value": null, "label": "None" }]""", false)]
    [InlineData("date", """[{ "value": "2026-10-22T00:00:00", "label": "Today" }]""", false)]
    [InlineData("date", InlineOptions, true)]
    public async Task Validate_ComparesValuesAsTheFrontend_ForOtherTypes(string field, string options, bool expectIssue)
    {
        var issues = await Validate(
            $$"""[{ "id": "radio", "type": "RadioButtons", "dataModelBindings": { "simpleBinding": "{{field}}" }, "options": {{options}} }]""",
            new OptionsTestModel
            {
                Number = 2.0m,
                Flag = false,
                Value = "null",
                Date = new DateTime(2026, 10, 22),
            }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Fact]
    public async Task Validate_GetsOptionsOnce_WithQueryParametersLikeTheFrontend()
    {
        var model = new OptionsTestModel
        {
            Value = "a",
            Municipality = "0301",
            Number = 2.0m,
            Flag = true,
            Date = new DateTime(2026, 10, 22),
            Rows = [new OptionsTestRow { Id = "r1" }],
        };
        var expectedParameters = new Dictionary<string, string>
        {
            ["language"] = "nb",
            ["municipality"] = "0301",
            ["number"] = "2",
            ["flag"] = "true",
            ["date"] = "2026-10-22T00:00:00",
            ["rows"] = JsonSerializer.Serialize(model.Rows),
            ["outside"] = "",
            ["static"] = "fixed",
            ["fromModel"] = "a",
            ["count"] = "5",
            ["empty"] = "",
            ["invalid"] = "",
        };
        SetupOptions("list", "a");
        var component = """
            "type": "Dropdown", "dataModelBindings": { "simpleBinding": "value" }, "optionsId": "list",
            "mapping": { "municipality": "municipality", "number": "number", "flag": "flag", "date": "date", "rows": "rows", "rows[{0}].id": "outside" },
            "queryParameters": { "static": "fixed", "fromModel": ["dataModel", "value"], "count": 5, "empty": ["dataModel", "note"], "invalid": { "not": "an expression" } }
            """;

        var issues = await Validate(
            $$"""[{ "id": "first", {{component}} }, { "id": "second", {{component}} }]""",
            model
        );

        Assert.Empty(issues);
        _appOptionsService.Verify(
            s =>
                s.GetOptionsAsync(
                    "list",
                    "nb",
                    It.Is<Dictionary<string, string>>(p =>
                        p.Count == expectedParameters.Count && !p.Except(expectedParameters).Any()
                    )
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData("\"mapping\": null, \"queryParameters\": null")]
    [InlineData("\"mapping\": [], \"queryParameters\": \"x\"")]
    [InlineData("\"mapping\": { \"value\": 1 }, \"queryParameters\": {}")]
    [InlineData("\"mapping\": { \"value\": \"a\", \"value\": \"b\" }, \"queryParameters\": { \"a\": 1, \"a\": 2 }")]
    public async Task Validate_ReadsMappingAndQueryParametersLeniently_WhenTheyAreNotObjectsOfStrings(string properties)
    {
        SetupOptions("list", "a");

        var issues = await Validate(
            $$"""[{ "id": "dropdown", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "value" }, "optionsId": "list", {{properties}} }]""",
            new OptionsTestModel { Value = "x" }
        );

        Assert.Single(issues);
    }

    [Fact]
    public async Task Validate_SendsMappedValuesFromTheRow_WhenComponentIsInRepeatingGroup()
    {
        SetupOptions("list", "a");

        await Validate(
            """
            [
              { "id": "group", "type": "RepeatingGroup", "dataModelBindings": { "group": "rows" }, "children": ["answer"] },
              { "id": "answer", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "rows.answer" }, "optionsId": "list", "mapping": { "rows[{0}].id": "rowId", "rows[{1}].id": "deeper" } }
            ]
            """,
            new OptionsTestModel
            {
                Rows = [new OptionsTestRow { Id = "r1", Answer = "a" }, new OptionsTestRow { Id = "r2", Answer = "a" }],
            }
        );

        foreach (var rowId in new[] { "r1", "r2" })
        {
            _appOptionsService.Verify(
                s =>
                    s.GetOptionsAsync(
                        "list",
                        "nb",
                        It.Is<Dictionary<string, string>>(p => p["rowId"] == rowId && p["deeper"] == "")
                    ),
                Times.Once
            );
        }
    }

    [Theory]
    [InlineData("a", false)]
    [InlineData("x", true)]
    public async Task Validate_UsesOptionsFromProvider_WhenOptionsIdIsSet(string value, bool expectIssue)
    {
        SetupOptions("list", "a");

        var issues = await Validate($"[{OptionsIdComponent}]", new OptionsTestModel { Value = value });

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Fact]
    public async Task Validate_UsesLanguageNb_WhenLanguageIsNull()
    {
        SetupOptions("list", "a");

        await Validate($"[{OptionsIdComponent}]", new OptionsTestModel { Value = "x" }, language: null);

        _appOptionsService.Verify(
            s => s.GetOptionsAsync("list", "nb", It.Is<Dictionary<string, string>>(p => p["language"] == "nb")),
            Times.Once
        );
    }

    [Theory]
    [InlineData("7", false)]
    [InlineData("10", false)]
    [InlineData("007", true)]
    [InlineData("n/a", false)]
    public async Task Validate_ComparesNumberOptionsAsTheFrontendReceivesThem_WhenProviderSetsValueTypeNumber(
        string value,
        bool expectIssue
    )
    {
        _appOptionsService
            .Setup(s => s.GetOptionsAsync("list", "nb", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(
                new AppOptions
                {
                    Options =
                    [
                        new AppOption
                        {
                            Value = "007",
                            ValueType = AppOptionValueType.Number,
                            Label = "Seven",
                        },
                        new AppOption
                        {
                            Value = "0010",
                            ValueType = AppOptionValueType.Number,
                            Label = "Ten",
                        },
                        new AppOption
                        {
                            Value = "n/a",
                            ValueType = AppOptionValueType.Number,
                            Label = "Not a number",
                        },
                    ],
                }
            );

        var issues = await Validate($"[{OptionsIdComponent}]", new OptionsTestModel { Value = value });

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Fact]
    public async Task Validate_ReturnsNoIssues_WhenOptionsAreNotFound()
    {
        _appOptionsService
            .Setup(s => s.GetOptionsAsync("list", "nb", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new AppOptions { Options = null });

        var issues = await Validate($"[{OptionsIdComponent}]", new OptionsTestModel { Value = "x" });

        Assert.Empty(issues);
    }

    public static TheoryData<Exception> ProviderExceptions =>
        new()
        {
            new HttpRequestException("down"),
            new TaskCanceledException("timeout"),
            new InvalidOperationException("bug"),
        };

    [Theory]
    [MemberData(nameof(ProviderExceptions))]
    public async Task Validate_ReturnsNoIssues_WhenOptionsProviderFails(Exception exception)
    {
        _appOptionsService
            .Setup(s => s.GetOptionsAsync("list", "nb", It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(exception);

        var issues = await Validate($"[{OptionsIdComponent}]", new OptionsTestModel { Value = "x" });

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData(true, "found", true)]
    [InlineData(true, "notFound", false)]
    [InlineData(true, "null", false)]
    [InlineData(false, "found", false)]
    public async Task Validate_UsesInstanceOptionsFirst_OnlyWhenSecure(
        bool secure,
        string instanceOptions,
        bool expectInstanceOptions
    )
    {
        _appOptionsService
            .Setup(s =>
                s.GetOptionsAsync(It.IsAny<InstanceIdentifier>(), "list", "nb", It.IsAny<Dictionary<string, string>>())
            )
            .ReturnsAsync(
                instanceOptions switch
                {
                    "found" => new AppOptions { Options = [new AppOption { Value = "instance", Label = "Instance" }] },
                    "notFound" => new AppOptions { Options = null },
                    _ => null,
                }
            );
        SetupOptions("list", "app");

        var issues = await Validate(
            $$"""[{ "id": "dropdown", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "value" }, "optionsId": "list", "secure": {{secure.ToString().ToLowerInvariant()}} }]""",
            new OptionsTestModel { Value = "instance" }
        );

        Assert.Equal(expectInstanceOptions ? 0 : 1, issues.Count);
        _appOptionsService.Verify(
            s => s.GetOptionsAsync("list", "nb", It.IsAny<Dictionary<string, string>>()),
            expectInstanceOptions ? Times.Never() : Times.Once()
        );
    }

    [Fact]
    public async Task Validate_UsesCodeListLibrary_WhenOptionsIdIsLibraryReference()
    {
        _altinn3LibraryCodeListService
            .Setup(s => s.GetAppOptionsAsync("ttd", "colors", "latest", "nb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppOptions { Options = [new AppOption { Value = "red", Label = "Red" }] });

        var issues = await Validate(
            """[{ "id": "dropdown", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "value" }, "optionsId": "lib**ttd**colors**latest" }]""",
            new OptionsTestModel { Value = "blue" }
        );

        Assert.Single(issues);
    }

    [Theory]
    [InlineData("r2", "rows[{0}].id", false)]
    [InlineData("", "rows[{0}].id", false)]
    [InlineData("r3", "rows[{0}].id", true)]
    [InlineData("r3", "value", false)]
    public async Task Validate_UsesRowValuesAsOptions_WhenOptionsComeFromSource(
        string value,
        string sourceValue,
        bool expectIssue
    )
    {
        var issues = await Validate(
            $$"""[{ "id": "radio", "type": "RadioButtons", "dataModelBindings": { "simpleBinding": "municipality" }, "source": { "group": "rows", "label": "x", "value": "{{sourceValue}}" } }]""",
            new OptionsTestModel
            {
                Municipality = value,
                Rows = [new OptionsTestRow { Id = "r1" }, new OptionsTestRow { Id = "r2" }, new OptionsTestRow()],
            }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Theory]
    [InlineData("""{ "group": "people", "label": "x", "value": "people[{0}].id" }""", "p1", false)]
    [InlineData("""{ "group": "people", "label": "x", "value": "people[{0}].id" }""", "x", true)]
    [InlineData("""{ "group": "rows.items", "label": "x", "value": "rows.items[{0}].id" }""", "i2", false)]
    [InlineData("""{ "group": "rows.items", "label": "x", "value": "rows.items[{0}].id" }""", "i1", true)]
    [InlineData(
        """{ "group": "rows.items", "label": "x", "value": "rows.items[{0}].id", "dataType": "default" }""",
        "i2",
        false
    )]
    public async Task Validate_ReadsSourceFromTheComponentsRow_WhenComponentIsInRepeatingGroup(
        string source,
        string value,
        bool expectIssue
    )
    {
        var issues = await Validate(
            $$"""
            [
              { "id": "group", "type": "RepeatingGroup", "dataModelBindings": { "group": "rows" }, "children": ["answer"] },
              { "id": "answer", "type": "Dropdown", "dataModelBindings": { "simpleBinding": "rows.answer" }, "source": {{source}} }
            ]
            """,
            new OptionsTestModel
            {
                People = [new OptionsTestRow { Id = "p1" }],
                Rows =
                [
                    new OptionsTestRow { Items = [new OptionsTestItem { Id = "i1" }] },
                    new OptionsTestRow { Answer = value, Items = [new OptionsTestItem { Id = "i2" }] },
                ],
            }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Theory]
    [InlineData("""{ "group": "rows", "label": "x", "value": "rows[{0}].id" }""", true)]
    [InlineData("""{ "group": "missing", "label": "x", "value": "missing[{0}].id" }""", false)]
    [InlineData("""{ "group": "rows", "label": "x", "value": "rows[{0}].id", "dataType": "missing" }""", false)]
    [InlineData("""{ "group": "rows", "label": "x" }""", false)]
    [InlineData("""{ "label": "x", "value": "rows[{0}].id" }""", false)]
    public async Task Validate_ChecksSourceOptions_OnlyWhenTheGroupCanBeRead(string source, bool expectIssue)
    {
        var issues = await Validate(
            $$"""[{ "id": "radio", "type": "RadioButtons", "dataModelBindings": { "simpleBinding": "municipality" }, "source": {{source}} }]""",
            new OptionsTestModel { Municipality = "r1", Rows = [] }
        );

        Assert.Equal(expectIssue ? 1 : 0, issues.Count);
    }

    [Fact]
    public void ShouldRunForTask_ReturnsTrue_OnlyForTasksInLayoutSets()
    {
        _appResources
            .Setup(r => r.GetLayoutSet())
            .Returns(
                new LayoutSets
                {
                    Sets =
                    [
                        new LayoutSet
                        {
                            Id = "form",
                            DataType = "default",
                            Tasks = ["Task_1"],
                        },
                    ],
                }
            );

        Assert.True(_validator.ShouldRunForTask("Task_1"));
        Assert.False(_validator.ShouldRunForTask("Task_2"));
    }

    [Fact]
    public void ShouldRunForTask_ReturnsFalse_WhenAppHasNoLayoutSets()
    {
        _appResources.Setup(r => r.GetLayoutSet()).Returns((LayoutSets?)null);

        Assert.False(_validator.ShouldRunForTask("Task_1"));
    }

    [Fact]
    public void ValidationSource_ReturnsOptions_ForAllTasks()
    {
        Assert.Equal("Options", _validator.ValidationSource);
        Assert.Equal("*", _validator.TaskId);
    }

    [Fact]
    public async Task HasRelevantChanges_ReturnsFalse_AsValidatorOnlyRunsOnFullValidation()
    {
        Assert.True(_validator.NoIncrementalValidation);
        Assert.False(
            await _validator.HasRelevantChanges(Mock.Of<IInstanceDataAccessor>(), "Task_1", new DataElementChanges([]))
        );
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void AddAppServices_RegistersOptionsValidator_OnlyWhenOptionsValidationIsOn(string? setting, bool expected)
    {
        var settings = setting is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?> { ["AppSettings:OptionsValidation"] = setting };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        services.AddAppServices(configuration, new Mock<IWebHostEnvironment>().Object);

        Assert.Equal(
            expected,
            services.Any(s => s.ServiceType == typeof(IValidator) && s.ImplementationType == typeof(OptionsValidator))
        );
    }

    private void SetupOptions(string optionsId, string value) =>
        _appOptionsService
            .Setup(s => s.GetOptionsAsync(optionsId, "nb", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new AppOptions { Options = [new AppOption { Value = value, Label = value }] });

    private async Task<List<ValidationIssue>> Validate(
        string components,
        OptionsTestModel model,
        string? language = "nb"
    )
    {
        var instance = new Instance
        {
            Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
            AppId = "org/app",
            Data = [],
        };
        var dataElement = new DataElement { Id = DataElementId, DataType = "default" };
        var dataType = new DataType { Id = "default" };
        var appMetadata = new ApplicationMetadata("org/app") { DataTypes = [dataType] };
        using var page = JsonDocument.Parse($$"""{ "data": { "layout": {{components}} } }""");
        var layout = new LayoutSetComponent(
            [PageComponent.Parse(page.RootElement, "page1", "layout")],
            "layout",
            dataType
        );
        var translationService = new TranslationService(
            new AppIdentifier("org", "app"),
            _appResources.Object,
            FakeLoggerXunit.Get<TranslationService>(_output)
        );
        var dataAccessor = new InstanceDataAccessorFake(
            instance,
            appMetadata,
            translationService,
            new LayoutModel([layout], null),
            new FrontEndSettings(),
            null,
            language
        )
        {
            { dataElement, model },
        };

        return await _validator.Validate(dataAccessor, "Task_1", language);
    }

    public class OptionsTestModel
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("number")]
        public decimal? Number { get; set; }

        [JsonPropertyName("flag")]
        public bool? Flag { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("rows")]
        public List<OptionsTestRow>? Rows { get; set; }

        [JsonPropertyName("people")]
        public List<OptionsTestRow>? People { get; set; }
    }

    public class OptionsTestRow
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("answer")]
        public string? Answer { get; set; }

        [JsonPropertyName("items")]
        public List<OptionsTestItem>? Items { get; set; }
    }

    public class OptionsTestItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
