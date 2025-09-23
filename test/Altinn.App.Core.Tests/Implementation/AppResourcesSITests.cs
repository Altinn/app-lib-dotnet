using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Implementation
{
    // Testing library and framework note:
    // These tests use xUnit as the test framework and Moq for mocking. We also rely on BCL assertions.
    public class AppResourcesSITests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly AppSettings _settings;
        private readonly Mock<IAppMetadata> _appMetadataMock;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AppResourcesSI> _logger;

        public AppResourcesSITests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "AppResourcesSI_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);

            _settings = new AppSettings
            {
                AppBasePath = _tempRoot,
                ConfigurationFolder = "config",
                TextFolder = "texts",
                ModelsFolder = "models",
                UiFolder = "ui",
                JsonSchemaFileName = "schema.json",
                FormLayoutSettingsFileName = "layout-settings.json",
                LayoutSetsFileName = "layout-sets.json",
                RuleConfigurationJSONFileName = "ruleconfig.json",
                RuleHandlerFileName = "rulehandler.js",
                FooterFileName = "footer.html",
                ValidationConfigurationFileName = "validation.json"
            };

            Directory.CreateDirectory(Path.Combine(_tempRoot, _settings.ConfigurationFolder, _settings.TextFolder));
            Directory.CreateDirectory(Path.Combine(_tempRoot, _settings.ModelsFolder));
            Directory.CreateDirectory(Path.Combine(_tempRoot, _settings.UiFolder));

            _appMetadataMock = new Mock<IAppMetadata>(MockBehavior.Strict);
            _env = Mock.Of<IWebHostEnvironment>();
            _logger = NullLogger<AppResourcesSI>.Instance;
        }

        private AppResourcesSI CreateSut(Telemetry? telemetry = null)
        {
            return new AppResourcesSI(
                Options.Create(_settings),
                _appMetadataMock.Object,
                _env,
                _logger,
                telemetry
            );
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempRoot))
                {
                    Directory.Delete(_tempRoot, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }

        [Fact(DisplayName = "GetText returns bytes when valid path and file exist")]
        public void GetText_ReturnsBytes_WhenFileExists()
        {
            // Arrange
            var sut = CreateSut();
            var folder = Path.Combine(_tempRoot, _settings.ConfigurationFolder, _settings.TextFolder);
            Directory.CreateDirectory(folder);

            var fileName = "resource.nb.json";
            var fullPath = Path.Combine(folder, fileName);
            var expected = Encoding.UTF8.GetBytes("{\"key\":\"value\"}");
            File.WriteAllBytes(fullPath, expected);

            // Note: GetText concatenates legalPath + filePath, so filePath must start with a separator.
            var filePathArg = Path.DirectorySeparatorChar + fileName;

            // Act
            var result = sut.GetText("org", "app", filePathArg);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }

        [Fact(DisplayName = "GetText throws ArgumentException for illegal file path")]
        public void GetText_Throws_ForIllegalPath()
        {
            // Arrange
            var sut = CreateSut();
            var filePathArg = ".." + Path.DirectorySeparatorChar + "evil.json";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => sut.GetText("org", "app", filePathArg));
        }

        [Fact(DisplayName = "GetTexts returns null when missing file")]
        public async Task GetTexts_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();

            var res = await sut.GetTexts("org", "app", "nb");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetTexts deserializes resource and sets metadata")]
        public async Task GetTexts_Deserializes_AndSets_Metadata()
        {
            // Arrange
            var sut = CreateSut();
            var folder = Path.Combine(_tempRoot, _settings.ConfigurationFolder, _settings.TextFolder);
            Directory.CreateDirectory(folder);

            var lang = "nb";
            var file = Path.Combine(folder, $"resource.{lang}.json");

            var textResource = new TextResource
            {
                Language = null,
                Org = null,
                Resources = new()
                {
                    new ResourceElement { Id = "key", Value = "value" }
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(textResource, new JsonSerializerOptions { WriteIndented = false });
            await File.WriteAllTextAsync(file, json, Encoding.UTF8);

            // Act
            var res = await sut.GetTexts("ttd", "myapp", lang);

            // Assert
            Assert.NotNull(res);
            Assert.Equal("ttd", res\!.Org);
            Assert.Equal(lang, res.Language);
            Assert.Equal("ttd-myapp-nb", res.Id);
            Assert.Single(res.Resources);
            Assert.Equal("key", res.Resources[0].Id);
            Assert.Equal("value", res.Resources[0].Value);
        }

        [Fact(DisplayName = "GetApplication returns Application mapped from ApplicationMetadata including OnEntry")]
        public void GetApplication_Returns_Mapped()
        {
            // Arrange
            var meta = new ApplicationMetadata
            {
                DataTypes = new List<DataType>(),
                OnEntry = new OnEntry { Show = "start" }
            };
            _appMetadataMock
                .Setup(m => m.GetApplicationMetadata())
                .Returns(Task.FromResult(meta));

            var sut = CreateSut();

            // Act
            var app = sut.GetApplication();

            // Assert
            Assert.NotNull(app);
            Assert.NotNull(app.OnEntry);
            Assert.Equal("start", app.OnEntry\!.Show);
        }

        [Fact(DisplayName = "GetApplication throws ApplicationConfigException on metadata failure")]
        public void GetApplication_Throws_OnFailure()
        {
            // Arrange
            _appMetadataMock
                .Setup(m => m.GetApplicationMetadata())
                .Returns(Task.FromException<ApplicationMetadata>(new InvalidOperationException("boom")));

            var sut = CreateSut();

            // Act & Assert
            var ex = Assert.Throws<ApplicationConfigException>(() => sut.GetApplication());
            Assert.Contains("Failed to read application metadata", ex.Message);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact(DisplayName = "GetApplicationXACMLPolicy returns policy on success")]
        public void GetApplicationXACMLPolicy_Returns_Policy()
        {
            // Arrange
            _appMetadataMock
                .Setup(m => m.GetApplicationXACMLPolicy())
                .Returns(Task.FromResult<string?>("<xacml/>"));

            var sut = CreateSut();

            // Act
            var policy = sut.GetApplicationXACMLPolicy();

            // Assert
            Assert.Equal("<xacml/>", policy);
        }

        [Fact(DisplayName = "GetApplicationXACMLPolicy returns null on failure")]
        public void GetApplicationXACMLPolicy_ReturnsNull_OnFailure()
        {
            // Arrange
            _appMetadataMock
                .Setup(m => m.GetApplicationXACMLPolicy())
                .Returns(Task.FromException<string?>(new Exception("fail")));

            var sut = CreateSut();

            // Act
            var policy = sut.GetApplicationXACMLPolicy();

            // Assert
            Assert.Null(policy);
        }

        [Fact(DisplayName = "GetApplicationBPMNProcess returns process on success")]
        public void GetApplicationBPMNProcess_Returns_Process()
        {
            // Arrange
            _appMetadataMock
                .Setup(m => m.GetApplicationBPMNProcess())
                .Returns(Task.FromResult<string?>("<bpmn/>"));

            var sut = CreateSut();

            // Act
            var process = sut.GetApplicationBPMNProcess();

            // Assert
            Assert.Equal("<bpmn/>", process);
        }

        [Fact(DisplayName = "GetApplicationBPMNProcess returns null on failure")]
        public void GetApplicationBPMNProcess_ReturnsNull_OnFailure()
        {
            // Arrange
            _appMetadataMock
                .Setup(m => m.GetApplicationBPMNProcess())
                .Returns(Task.FromException<string?>(new Exception("fail")));

            var sut = CreateSut();

            // Act
            var process = sut.GetApplicationBPMNProcess();

            // Assert
            Assert.Null(process);
        }

        [Fact(DisplayName = "GetModelJsonSchema returns file content")]
        public void GetModelJsonSchema_Returns_FileContent()
        {
            // Arrange
            var sut = CreateSut();
            var modelId = "myModel";
            var modelFolder = Path.Combine(_tempRoot, _settings.ModelsFolder);
            Directory.CreateDirectory(modelFolder);
            var schemaFile = Path.Combine(modelFolder, $"{modelId}.{_settings.JsonSchemaFileName}");
            File.WriteAllText(schemaFile, "{\"$schema\":\"http://json-schema.org/draft-07/schema#\"}", Encoding.UTF8);

            // Act
            var s = sut.GetModelJsonSchema(modelId);

            // Assert
            Assert.Contains("\"$schema\"", s);
        }

        [Fact(DisplayName = "GetPrefillJson returns null when prefill file missing")]
        public void GetPrefillJson_ReturnsNull_Missing()
        {
            var sut = CreateSut();
            var res = sut.GetPrefillJson();
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetPrefillJson returns content when file exists")]
        public void GetPrefillJson_ReturnsContent_WhenExists()
        {
            var sut = CreateSut();
            var folder = Path.Combine(_tempRoot, _settings.ModelsFolder);
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "ServiceModel.prefill.json");
            File.WriteAllText(file, "{\"a\":1}", Encoding.UTF8);

            var res = sut.GetPrefillJson();

            Assert.Equal("{\"a\":1}", res);
        }

        [Fact(DisplayName = "GetLayoutSettingsString returns null when missing")]
        public void GetLayoutSettingsString_ReturnsNull_Missing()
        {
            var sut = CreateSut();
            var res = sut.GetLayoutSettingsString();
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetLayoutSettingsString returns content when exists")]
        public void GetLayoutSettingsString_ReturnsContent_WhenExists()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.FormLayoutSettingsFileName);
            File.WriteAllText(file, "{\"pages\":{\"order\":[\"page1\"]}}", Encoding.UTF8);

            var res = sut.GetLayoutSettingsString();

            Assert.Equal("{\"pages\":{\"order\":[\"page1\"]}}", res);
        }

        [Fact(DisplayName = "GetLayoutSettings returns deserialized layout settings")]
        public void GetLayoutSettings_Returns_Deserialized()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.FormLayoutSettingsFileName);
            File.WriteAllText(file, "{\"pages\":{\"order\":[\"p1\",\"p2\"]}}", Encoding.UTF8);

            var settings = sut.GetLayoutSettings();

            Assert.NotNull(settings);
            Assert.NotNull(settings.Pages);
            Assert.NotNull(settings.Pages.Order);
            Assert.Equal(new[] { "p1", "p2" }, settings.Pages.Order);
        }

        [Fact(DisplayName = "GetLayoutSettings throws when file is missing")]
        public void GetLayoutSettings_Throws_WhenMissing()
        {
            var sut = CreateSut();
            Assert.Throws<FileNotFoundException>(() => sut.GetLayoutSettings());
        }

        [Fact(DisplayName = "GetClassRefForLogicDataType returns classRef when found")]
        public void GetClassRefForLogicDataType_Returns_ClassRef_WhenFound()
        {
            var meta = new ApplicationMetadata
            {
                DataTypes = new List<DataType>
                {
                    new DataType
                    {
                        Id = "model-1",
                        AppLogic = new ApplicationLogic { ClassRef = "Altinn.App.Models.Model1" }
                    }
                }
            };
            _appMetadataMock.Setup(m => m.GetApplicationMetadata()).Returns(Task.FromResult(meta));

            var sut = CreateSut();

            var cls = sut.GetClassRefForLogicDataType("model-1");
            Assert.Equal("Altinn.App.Models.Model1", cls);
        }

        [Fact(DisplayName = "GetClassRefForLogicDataType returns empty string when not found")]
        public void GetClassRefForLogicDataType_Returns_Empty_WhenNotFound()
        {
            var meta = new ApplicationMetadata
            {
                DataTypes = new List<DataType>
                {
                    new DataType { Id = "another", AppLogic = new ApplicationLogic { ClassRef = "X" } }
                }
            };
            _appMetadataMock.Setup(m => m.GetApplicationMetadata()).Returns(Task.FromResult(meta));

            var sut = CreateSut();

            var cls = sut.GetClassRefForLogicDataType("missing");
            Assert.Equal(string.Empty, cls);
        }

        [Fact(DisplayName = "GetLayouts returns legacy FormLayout when FormLayout.json exists")]
        public void GetLayouts_Returns_Legacy_FormLayout()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var legacy = Path.Combine(ui, "FormLayout.json");
            File.WriteAllText(legacy, "{\"some\":\"layout\"}", Encoding.UTF8);

            var json = sut.GetLayouts();

            Assert.Contains("\"FormLayout\"", json);
            Assert.Contains("\"some\":\"layout\"", json);
        }

        [Fact(DisplayName = "GetLayouts returns empty object when layouts folder missing")]
        public void GetLayouts_Returns_Empty_WhenNoLayouts()
        {
            var sut = CreateSut();

            var json = sut.GetLayouts();

            // Expect "{}" or an empty object representation
            Assert.Contains("{", json);
            // Should not contain FormLayout unless present
            Assert.DoesNotContain("FormLayout", json);
        }

        [Fact(DisplayName = "GetLayouts aggregates files from ui/layouts")]
        public void GetLayouts_Aggregates_Files()
        {
            var sut = CreateSut();
            var layoutsPath = Path.Combine(_tempRoot, _settings.UiFolder, "layouts");
            Directory.CreateDirectory(layoutsPath);

            File.WriteAllText(Path.Combine(layoutsPath, "page1.json"), "{\"a\":1}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(layoutsPath, "page2.json"), "{\"b\":2}", Encoding.UTF8);

            var json = sut.GetLayouts();

            Assert.Contains("page1", json);
            Assert.Contains("page2", json);
            Assert.Contains("\"a\":1", json);
            Assert.Contains("\"b\":2", json);
        }

        [Fact(DisplayName = "GetLayoutSets returns null when file is missing")]
        public void GetLayoutSets_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetLayoutSets();
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetLayoutSets returns content when file exists")]
        public void GetLayoutSets_Returns_Content_WhenExists()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.LayoutSetsFileName);
            File.WriteAllText(file, "{\"sets\":[{\"id\":\"set1\",\"tasks\":[\"Task_1\"],\"dataType\":\"dt1\"}]}", Encoding.UTF8);

            var res = sut.GetLayoutSets();

            Assert.Equal("{\"sets\":[{\"id\":\"set1\",\"tasks\":[\"Task_1\"],\"dataType\":\"dt1\"}]}", res);
        }

        [Fact(DisplayName = "GetLayoutSet parses layout sets when content exists")]
        public void GetLayoutSet_Parses_WhenExists()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.LayoutSetsFileName);
            File.WriteAllText(file, "{\"sets\":[{\"id\":\"set1\",\"tasks\":[\"Task_1\"],\"dataType\":\"dt1\"}]}", Encoding.UTF8);

            var res = sut.GetLayoutSet();

            Assert.NotNull(res);
            Assert.Single(res\!.Sets);
            Assert.Equal("set1", res.Sets[0].Id);
        }

        [Fact(DisplayName = "GetLayoutSet returns null when no layout sets")]
        public void GetLayoutSet_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            Assert.Null(sut.GetLayoutSet());
        }

        [Fact(DisplayName = "GetLayoutSetForTask returns the set that contains the task")]
        public void GetLayoutSetForTask_Returns_Set_ForTask()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.LayoutSetsFileName);
            File.WriteAllText(file, "{\"sets\":[{\"id\":\"set1\",\"tasks\":[\"Task_A\"],\"dataType\":\"dt1\"},{\"id\":\"set2\",\"tasks\":[\"Task_B\"],\"dataType\":\"dt2\"}]}", Encoding.UTF8);

            var res = sut.GetLayoutSetForTask("Task_B");

            Assert.NotNull(res);
            Assert.Equal("set2", res\!.Id);
        }

        [Fact(DisplayName = "GetLayoutSetForTask returns null when task not found")]
        public void GetLayoutSetForTask_ReturnsNull_WhenNotFound()
        {
            var sut = CreateSut();
            var ui = Path.Combine(_tempRoot, _settings.UiFolder);
            Directory.CreateDirectory(ui);
            var file = Path.Combine(ui, _settings.LayoutSetsFileName);
            File.WriteAllText(file, "{\"sets\":[{\"id\":\"set1\",\"tasks\":[\"Task_A\"],\"dataType\":\"dt1\"}]}", Encoding.UTF8);

            var res = sut.GetLayoutSetForTask("Task_X");

            Assert.Null(res);
        }

        [Fact(DisplayName = "GetLayoutsForSet returns aggregated pages for the set")]
        public void GetLayoutsForSet_Returns_Pages()
        {
            var sut = CreateSut();

            var basePath = Path.Combine(_tempRoot, _settings.UiFolder, "set1", "layouts");
            Directory.CreateDirectory(basePath);
            File.WriteAllText(Path.Combine(basePath, "a.json"), "{\"x\":1}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(basePath, "b.json"), "{\"y\":2}", Encoding.UTF8);

            var json = sut.GetLayoutsForSet("set1");

            Assert.Contains("a", json);
            Assert.Contains("b", json);
            Assert.Contains("\"x\":1", json);
            Assert.Contains("\"y\":2", json);
        }

        [Fact(DisplayName = "GetLayoutSettingsStringForSet returns null when missing")]
        public void GetLayoutSettingsStringForSet_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetLayoutSettingsStringForSet("set1");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetLayoutSettingsStringForSet returns content when exists")]
        public void GetLayoutSettingsStringForSet_Returns_Content_WhenExists()
        {
            var sut = CreateSut();

            var file = Path.Combine(_tempRoot, _settings.UiFolder, "setX", _settings.FormLayoutSettingsFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(file)\!);
            File.WriteAllText(file, "{\"pages\":{\"order\":[\"p\"]}}", Encoding.UTF8);

            var res = sut.GetLayoutSettingsStringForSet("setX");
            Assert.Equal("{\"pages\":{\"order\":[\"p\"]}}", res);
        }

        [Fact(DisplayName = "GetLayoutSettingsForSet returns deserialized settings when file exists")]
        public void GetLayoutSettingsForSet_Returns_Deserialized_WhenExists()
        {
            var sut = CreateSut();

            var file = Path.Combine(_tempRoot, _settings.UiFolder, "setY", _settings.FormLayoutSettingsFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(file)\!);
            File.WriteAllText(file, "{\"pages\":{\"order\":[\"p1\",\"p2\"]}}", Encoding.UTF8);

            var res = sut.GetLayoutSettingsForSet("setY");

            Assert.NotNull(res);
            Assert.NotNull(res\!.Pages);
            Assert.Equal(new[] { "p1", "p2" }, res.Pages\!.Order);
        }

        [Fact(DisplayName = "GetLayoutSettingsForSet returns null when file missing")]
        public void GetLayoutSettingsForSet_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetLayoutSettingsForSet("setZ");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetRuleConfigurationForSet returns bytes when file exists")]
        public void GetRuleConfigurationForSet_ReturnsBytes_WhenExists()
        {
            var sut = CreateSut();

            var dir = Path.Combine(_tempRoot, _settings.UiFolder, "set1");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, _settings.RuleConfigurationJSONFileName);
            var content = Encoding.UTF8.GetBytes("{\"rule\":true}");
            File.WriteAllBytes(file, content);

            var res = sut.GetRuleConfigurationForSet("set1");

            Assert.NotNull(res);
            Assert.Equal(content, res);
        }

        [Fact(DisplayName = "GetRuleConfigurationForSet returns null when file missing")]
        public void GetRuleConfigurationForSet_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetRuleConfigurationForSet("nope");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetRuleHandlerForSet returns bytes when file exists")]
        public void GetRuleHandlerForSet_ReturnsBytes_WhenExists()
        {
            var sut = CreateSut();

            var dir = Path.Combine(_tempRoot, _settings.UiFolder, "set1");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, _settings.RuleHandlerFileName);
            var content = Encoding.UTF8.GetBytes("function handler(){}");
            File.WriteAllBytes(file, content);

            var res = sut.GetRuleHandlerForSet("set1");

            Assert.NotNull(res);
            Assert.Equal(content, res);
        }

        [Fact(DisplayName = "GetRuleHandlerForSet returns null when file missing")]
        public void GetRuleHandlerForSet_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetRuleHandlerForSet("nope");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetFooter returns null when missing")]
        public async Task GetFooter_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = await sut.GetFooter();
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetFooter returns content when exists")]
        public async Task GetFooter_ReturnsContent_WhenExists()
        {
            var sut = CreateSut();
            var file = Path.Combine(_tempRoot, _settings.UiFolder, _settings.FooterFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(file)\!);
            await File.WriteAllTextAsync(file, "<footer/>", Encoding.UTF8);

            var res = await sut.GetFooter();

            Assert.Equal("<footer/>", res);
        }

        [Fact(DisplayName = "GetValidationConfiguration returns null when missing")]
        public void GetValidationConfiguration_ReturnsNull_WhenMissing()
        {
            var sut = CreateSut();
            var res = sut.GetValidationConfiguration("dtX");
            Assert.Null(res);
        }

        [Fact(DisplayName = "GetValidationConfiguration returns content when exists")]
        public void GetValidationConfiguration_ReturnsContent_WhenExists()
        {
            var sut = CreateSut();
            var dir = Path.Combine(_tempRoot, _settings.ModelsFolder);
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"dt1.{_settings.ValidationConfigurationFileName}");
            File.WriteAllText(file, "{\"rules\":[]}", Encoding.UTF8);

            var res = sut.GetValidationConfiguration("dt1");
            Assert.Equal("{\"rules\":[]}", res);
        }

        // Note: GetLayoutModel and GetLayoutModelForTask require substantial on-disk layout/page JSONs and settings.
        // We focus tests on higher-value coverage above (I/O and metadata). An optional minimal test for GetLayoutModelForTask follows.

        [Fact(DisplayName = "GetLayoutModelForTask returns null when no layout sets")]
        public void GetLayoutModelForTask_ReturnsNull_WhenNoLayoutSets()
        {
            var meta = new ApplicationMetadata { DataTypes = new List<DataType>() };
            _appMetadataMock.Setup(m => m.GetApplicationMetadata()).Returns(Task.FromResult(meta));

            var sut = CreateSut();

            var res = sut.GetLayoutModelForTask("Task_1");
            Assert.Null(res);
        }
    }
}
