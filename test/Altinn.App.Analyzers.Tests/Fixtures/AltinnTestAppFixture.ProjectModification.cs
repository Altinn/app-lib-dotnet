using Microsoft.CodeAnalysis;

namespace Altinn.App.Analyzers.Tests.Fixtures;

partial class AltinnTestAppFixture
{
    private sealed record ProjectModification : IDisposable
    {
        private readonly AltinnTestAppFixture _fixture;
        private readonly AdhocWorkspace _workspace;
        private readonly Project _project;
        private readonly Action? _action;

        internal ProjectModification(AltinnTestAppFixture fixture, Action? action = null)
        {
            if (!fixture._isInitialized)
                throw new InvalidOperationException("Fixture not initialized");

            _fixture = fixture;
            _workspace = fixture._workspace;
            _project = fixture._project;
            _action = action;
        }

        public void Dispose()
        {
            _fixture._workspace = _workspace;
            _fixture._project = _project;
            _action?.Invoke();
        }
    }
}
