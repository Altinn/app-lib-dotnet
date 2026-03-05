using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.DataProcessing;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Tests.LayoutExpressions.TestUtilities;
using Altinn.App.Core.Tests.TestUtils;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;
using IAppResources = Altinn.App.Core.Internal.App.IAppResources;

namespace Altinn.App.Core.Tests.Features.DataProcessing;

public class DataFieldValueCalculatorTests
{
    private readonly ITestOutputHelper _output;
    private readonly DataFieldValueCalculator _dataFieldValueCalculator;
    private readonly FakeLogger<DataFieldValueCalculator> _logger = new();
    private readonly Mock<IAppResources> _appResources = new(MockBehavior.Strict);
    private readonly Mock<ILayoutEvaluatorStateInitializer> _layoutInitializer = new(MockBehavior.Strict);
    private readonly IOptions<FrontEndSettings> _frontendSettings = Microsoft.Extensions.Options.Options.Create(
        new FrontEndSettings()
    );
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DataFieldValueCalculatorTests(ITestOutputHelper output)
    {
        var accessCheckerMock = new Mock<IDataElementAccessChecker>();
        accessCheckerMock.Setup(x => x.CanRead(It.IsAny<Instance>(), It.IsAny<DataType>())).ReturnsAsync(true);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IDataElementAccessChecker)))
            .Returns(accessCheckerMock.Object);

        _output = output;
        _dataFieldValueCalculator = new DataFieldValueCalculator(
            _logger,
            _layoutInitializer.Object,
            _appResources.Object,
            serviceProviderMock.Object
        );
    }

    public async Task<DataFieldValueCalculatorTestModel> LoadData(string fileName, string folder)
    {
        var data = await File.ReadAllTextAsync(Path.Join(folder, fileName));
        _output.WriteLine(data);
        return JsonSerializer.Deserialize<DataFieldValueCalculatorTestModel>(data, _jsonSerializerOptions)!;
    }

    [Theory]
    [FileNamesInFolderData(["Features", "DataProcessing", "data-field-value-calculator-tests", "backend"])]
    public async Task RunDataFieldCalculationTestsForBackend(string fileName, string folder)
    {
        var (result, testCase) = await RunDataFieldCalculatorTest(fileName, folder);

        foreach (var expected in testCase.Expects)
        {
            result.Get(expected.Field).Should().Be(expected.Result.ToObject());
        }
    }

    [Theory]
    [FileNamesInFolderData(["Features", "DataProcessing", "data-field-value-calculator-tests", "shared"])]
    public async Task RunDataFieldCalculationTestsForShared(string fileName, string folder)
    {
        var (result, testCase) = await RunDataFieldCalculatorTest(fileName, folder);

        foreach (var expected in testCase.Expects)
        {
            result.Get(expected.Field).Should().Be(expected.Result.ToObject());
        }
    }

    [Fact]
    public async Task ShouldLogWarningWhenTryingToSetUnsupportedDataType()
    {
        var testCaseJson = """
            {
                "name": "Should log warning when trying to set field with unsupported data type",
                "expects": [
                    {
                        "logMessageWarning": "Could not set calculated value for field form.unsupportedDataType in data element 30844cc0-81af-4429-9f9e-035d78f1f9da. This is because the type conversion failed."
                    }
                ],
                "calculationConfig": {
                    "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/validation/validation.schema.v1.json",
                    "calculations": {
                        "form.unsupportedDataType": ["four-times-two"]
                    },
                    "definitions": {
                        "four-times-two": {
                            "condition": ["language"]
                        }
                    }
                },
                "formData": {
                    "form": {
                        "unsupportedDataType": true
                    }
                },
                "layouts": {
                    "Page": {
                        "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/layout/layout.schema.v1.json",
                        "data": {
                            "layout": [
                            ]
                        }
                    }
                }
            }
            """;
        _output.WriteLine(testCaseJson);
        var testCase = JsonSerializer.Deserialize<DataFieldValueCalculatorTestModel>(
            testCaseJson,
            _jsonSerializerOptions
        )!;

        await RunDataFieldCalculatorTest(testCase);

        foreach (var expected in testCase.Expects)
        {
            _logger.Collector.GetSnapshot().Select(x => x.Message).Should().Contain(expected.LogMessageWarning);
        }
    }

    private async Task<(IFormDataWrapper, DataFieldValueCalculatorTestModel)> RunDataFieldCalculatorTest(
        string fileName,
        string folder
    )
    {
        var testCase = await LoadData(fileName, folder);

        return (await RunDataFieldCalculatorTest(testCase), testCase);
    }

    private async Task<IFormDataWrapper> RunDataFieldCalculatorTest(DataFieldValueCalculatorTestModel testCase)
    {
        var instance = new Instance() { Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d", AppId = "org/app" };
        var dataElement = new DataElement { Id = "30844cc0-81af-4429-9f9e-035d78f1f9da", DataType = "default" };
        var dataType = new DataType() { Id = "default" };

        var dataAccessor = DynamicClassBuilder.DataAccessorFromJsonDocument(instance, testCase.FormData, dataElement);

        var layout = new LayoutSetComponent(testCase.Layouts, "layout", dataType);
        var componentModel = new LayoutModel([layout], null);
        var translationService = new TranslationService(
            new AppIdentifier("org", "app"),
            _appResources.Object,
            FakeLoggerXunit.Get<TranslationService>(_output)
        );
        var evaluatorState = new LayoutEvaluatorState(
            dataAccessor,
            componentModel,
            translationService,
            _frontendSettings.Value
        );
        _layoutInitializer
            .Setup(init =>
                init.Init(It.IsAny<IInstanceDataAccessor>(), "Task_1", It.IsAny<string?>(), It.IsAny<string?>())
            )
            .ReturnsAsync(evaluatorState);

        _appResources
            .Setup(ar => ar.GetTexts("org", "app", "nb"))
            .ReturnsAsync(
                testCase.TextResources is null
                    ? null
                    : new TextResource { Language = "nb", Resources = testCase.TextResources }
            );

        await _dataFieldValueCalculator.CalculateFormData(
            dataAccessor,
            dataElement,
            "Task_1",
            JsonSerializer.Serialize(testCase.CalculationConfig)
        );

        return await dataAccessor.GetFormDataWrapper(dataElement);
    }

    public record DataFieldValueCalculatorTestModel
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("expects")]
        public required Expected[] Expects { get; set; }

        [JsonPropertyName("calculationConfig")]
        public required JsonElement CalculationConfig { get; set; }

        [JsonPropertyName("formData")]
        public required JsonElement FormData { get; set; }

        [JsonPropertyName("layouts")]
        public required IReadOnlyDictionary<string, JsonElement> Layouts { get; set; }

        [JsonPropertyName("textResources")]
        public List<TextResourceElement>? TextResources { get; set; }
    }

    public record Expected
    {
        public string Field { get; set; }

        public ExpressionValue Result { get; set; }

        public string LogMessageWarning { get; set; }
    }
}
