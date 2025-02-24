using Argon;
using Xunit.Abstractions;

namespace Altinn.App.Integration.FrontendTests;

[Collection("FrontendTestCollection")]
public class VerifyOpenApi
{
    private readonly ITestOutputHelper _output;
    private readonly FrontendTestFixture _fixture;

    public VerifyOpenApi(ITestOutputHelper output, FrontendTestFixture fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    [Fact]
    public async Task CustomOpenApi()
    {
        var client = _fixture.GetAppClient();
        var responseMessage = await client.GetAsync("/ttd/frontend-test/v1/customOpenapi.json");
        var response = await responseMessage.Content.ReadAsStringAsync();
        // _output.WriteLine(response);
        responseMessage.EnsureSuccessStatusCode();
        await VerifyJson(response, _verifySettings).UseFileName("customOpenapi");
    }

    [Fact]
    public async Task GenericOpenApi()
    {
        var client = _fixture.GetAppClient();
        var responseMessage = await client.GetAsync("/ttd/frontend-test/swagger/v1/swagger.json");
        var response = await responseMessage.Content.ReadAsStringAsync();
        // _output.WriteLine(response);
        responseMessage.EnsureSuccessStatusCode();
        await VerifyJson(response, _verifySettings).UseFileName("genericOpenapi");
    }

    private static VerifySettings _verifySettings
    {
        get
        {
            VerifySettings settings = new();
            settings.UseStrictJson();
            settings.DontScrubGuids();
            settings.DontIgnoreEmptyCollections();
            settings.AddExtraSettings(settings => settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore);
            return settings;
        }
    }
}
