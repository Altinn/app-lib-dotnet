using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Altinn.App.Analyzers.Tests.Fixtures;

partial class AltinnTestAppFixture
{
    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

        public static TestOptionsProvider Empty { get; } =
            new TestOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                TestAnalyzerConfigOptions.Empty
            );

        internal TestOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions
        )
        {
            _treeDict = treeDict;
            GlobalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            _treeDict.TryGetValue(tree, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            _treeDict.TryGetValue(textFile, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        internal TestOptionsProvider WithAdditionalTreeOptions(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict
        ) => new TestOptionsProvider(_treeDict.AddRange(treeDict), GlobalOptions);

        internal TestOptionsProvider WithGlobalOptions(AnalyzerConfigOptions globalOptions) =>
            new TestOptionsProvider(_treeDict, globalOptions);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        internal static readonly ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<
            string,
            string
        >(KeyComparer);

        public static TestAnalyzerConfigOptions Empty { get; } = new TestAnalyzerConfigOptions(EmptyDictionary);

        internal readonly ImmutableDictionary<string, string> Options;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options) => Options = options;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            Options.TryGetValue(key, out value);

        public override IEnumerable<string> Keys => Options.Keys;
    }
}
