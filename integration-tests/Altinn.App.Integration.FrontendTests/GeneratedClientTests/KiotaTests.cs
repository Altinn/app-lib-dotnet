using Altinn.App.Integration.FrontendTests.GeneratedClient;
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;
using Xunit.Abstractions;

namespace Altinn.App.Integration.FrontendTests.GeneratedClientTests;

[Collection("FrontendTestCollection")]
public class KiotaTests
{
    private readonly ITestOutputHelper _output;
    private readonly FrontendTestFixture _fixture;

    public KiotaTests(ITestOutputHelper output, FrontendTestFixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    [Fact]
    public async Task RunMultipleSteps()
    {
        var testRunner = new FrontendTestsRunner(_fixture.GetAppClient(), _output.WriteLine);
        await testRunner.RunMultipleSteps();
    }
}
