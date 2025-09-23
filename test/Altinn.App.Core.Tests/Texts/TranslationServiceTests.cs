using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Altinn.App.Core.Tests.Texts
{
    public class TranslationServiceTests
    {
        private static ITranslationService CreateService(Func<string,string,Task<TextResource?>> getTexts)
        {
            var appIdentifier = new AppIdentifier("ttd", "app");
            var appResources = new TestAppResources(getTexts);
            var logger = NullLogger<TranslationService>.Instance;
            // instantiate internal type via reflection to avoid InternalsVisibleTo requirement
            var implType = typeof(ITranslationService).Assembly.GetType("Altinn.App.Core.Internal.Texts.TranslationService", throwOnError: true)\!;
            var service = (ITranslationService)Activator.CreateInstance(implType, appIdentifier, appResources, logger)\!;
            return service;
        }

        private class TestAppResources : IAppResources
        {
            private readonly Func<string,string,Task<TextResource?>> _getTexts;
            public TestAppResources(Func<string,string,Task<TextResource?>> getTexts) => _getTexts = getTexts;
            public Task<TextResource?> GetTexts(string org, string app, string language) => _getTexts(org, language);
        }

        private static TextResource TR(string lang, params TextResourceElement[] elements)
            => new TextResource { Language = lang, Resources = new List<TextResourceElement>(elements) };

        private static TextResourceElement TRE(string id, string value, params TextResourceVariable[] variables)
            => new TextResourceElement { Id = id, Value = value, Variables = variables?.Length > 0 ? new List<TextResourceVariable>(variables) : null };

        private static TextResourceVariable Var(string key, string dataSource, string? defaultValue = null)
            => new TextResourceVariable { Key = key, DataSource = dataSource, DefaultValue = defaultValue };

        [Fact]
        public async Task TranslateTextKey_ReturnsValue_WhenKeyExists_InRequestedLanguage()
        {
            // Arrange
            var textsNb = TR("nb", TRE("welcome", "Velkommen"));
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateTextKey("welcome", "nb");

            // Assert
            Assert.Equal("Velkommen", result);
        }

        [Fact]
        public async Task TranslateTextKey_FallbacksToNb_WhenLanguageMissing()
        {
            // Arrange
            var textsNb = TR("nb", TRE("greet", "Hei"));
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateTextKey("greet", "en");

            // Assert
            Assert.Equal("Hei", result);
        }

        [Fact]
        public async Task TranslateTextKey_DefaultLanguageIsNb_WhenNullProvided()
        {
            // Arrange
            var textsNb = TR("nb", TRE("title", "Tittel"));
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateTextKey("title", null);

            // Assert
            Assert.Equal("Tittel", result);
        }

        [Fact]
        public async Task TranslateTextKey_PerformsVariableReplacement_CustomTextParameters()
        {
            // Arrange: {0} and {1} from customTextParameters; state/context are null for this overload
            var elem = TRE(
                id: "greeting",
                value: "Hello {0}, today is {1}.",
                Var("name", "customTextParameters"),
                Var("date", "customTextParameters")
            );
            var textsEn = TR("en", elem);
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "en" ? textsEn : null));

            var custom = new Dictionary<string, string> { ["name"] = "Alice", ["date"] = "Monday" };

            // Act
            var result = await service.TranslateTextKey("greeting", "en", custom);

            // Assert
            Assert.Equal("Hello Alice, today is Monday.", result);
        }

        [Fact]
        public async Task TranslateTextKey_VariableDefaultValueUsed_WhenUnsupportedDataSourceAndDefaultProvided()
        {
            // Arrange: state/context null; for 'dataModel.*' EvaluateTextVariable returns null -> DefaultValue used
            var elem = TRE(
                id: "label",
                value: "Value: {0}",
                Var("modelField", "dataModel.default", defaultValue: "42")
            );
            var textsNb = TR("nb", elem);
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateTextKey("label", "nb");

            // Assert
            Assert.Equal("Value: 42", result);
        }

        [Fact]
        public async Task TranslateTextKey_VariableKeyUsed_WhenUnsupportedDataSourceAndNoDefaultProvided()
        {
            // Arrange: If EvaluateTextVariable returns null and DefaultValue is null -> variable.Key is used
            var elem = TRE(
                id: "label2",
                value: "Missing: {0}",
                Var("missingField", "instanceContext", defaultValue: null)
            );
            var textsNb = TR("nb", elem);
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateTextKey("label2", "nb");

            // Assert
            Assert.Equal("Missing: missingField", result);
        }

        [Fact]
        public async Task TranslateFirstMatchingTextKey_ReturnsFirstFound_InOrder()
        {
            // Arrange
            var textsNb = TR("nb",
                TRE("a", "A"),
                TRE("b", "B")
            );
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateFirstMatchingTextKey("nb", "x", "b", "a");

            // Assert
            Assert.Equal("B", result);
        }

        [Fact]
        public async Task TranslateFirstMatchingTextKey_FallbackToNb_WhenLanguageSet_AndMissing()
        {
            // Arrange
            var textsNb = TR("nb",
                TRE("k1", "NB value")
            );
            var service = CreateService((_, lang) =>
                Task.FromResult<TextResource?>(lang == "nb" ? textsNb : null));

            // Act
            var result = await service.TranslateFirstMatchingTextKey("en", "k1");

            // Assert
            Assert.Equal("NB value", result);
        }

        [Fact]
        public async Task TranslateTextKey_ReturnsBackendFallback_Required_Error_Nb_And_Nn_And_DefaultEn()
        {
            // Arrange: No text resources at all -> trigger backend fallback path
            var service = CreateService((_, __) => Task.FromResult<TextResource?>(null));

            // Act
            var nb = await service.TranslateTextKey("backend.validation_errors.required", "nb");
            var nn = await service.TranslateTextKey("backend.validation_errors.required", "nn");
            var en = await service.TranslateTextKey("backend.validation_errors.required", "en");

            // Assert
            Assert.Equal("Feltet er påkrevd", nb);
            Assert.Equal("Feltet er påkravd", nn);
            Assert.Equal("Field is required", en);
        }

        [Fact]
        public async Task TranslateTextKeyLenient_ReturnsNull_WhenKeyIsNullOrEmpty()
        {
            // Arrange
            var service = CreateService((_, __) => Task.FromResult<TextResource?>(null));

            // Act
            var r1 = await service.TranslateTextKeyLenient(null, "nb");
            var r2 = await service.TranslateTextKeyLenient("", "nb");

            // Assert
            Assert.Null(r1);
            Assert.Null(r2);
        }
    }
}