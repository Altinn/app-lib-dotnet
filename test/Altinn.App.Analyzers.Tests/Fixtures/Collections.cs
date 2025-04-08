namespace Altinn.App.Analyzers.Tests.Fixtures;

// This fixture is used to provide a test app Roslyn workspace for the analyzers to run on.
// The test app is a real blank Altinn app in the "testapp/" folder.
// Initializing the fixture is expensive, and can take anywhere between 5-20 seconds on my machine currently,
// so currently tests run in a "global collection" to avoid re-initializing the fixture for each test.
// It also gives us some flexibility in that we can make physical changes to project files.
[CollectionDefinition(nameof(AltinnTestAppCollection), DisableParallelization = true)]
public class AltinnTestAppCollection : ICollectionFixture<AltinnTestAppFixture> { }

// This fixture is meant to provide a workspace for injecting code into Altinn.App.Core
// to test internal analyzers.
// Note that DisableParallelization is set to true as both fixtures rely on project references
// to Altinn.App.Core. If tests using these collections run in parallel we will have a race condition
// between the Roslyn workspaces as they will both try to build Altinn.App.Core at the same time.
[CollectionDefinition(nameof(AltinnAppCoreCollection), DisableParallelization = true)]
public class AltinnAppCoreCollection : ICollectionFixture<AltinnAppCoreFixture> { }
