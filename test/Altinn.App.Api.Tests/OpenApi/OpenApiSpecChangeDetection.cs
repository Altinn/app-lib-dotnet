using Argon;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.OpenApi.Readers;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.OpenApi;

public class OpenApiSpecChangeDetection : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public OpenApiSpecChangeDetection(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    [Fact]
    public async Task SaveJsonSwagger()
    {
        using HttpClient client = GetRootedClient("tdd", "contributer-restriction");
        // The test project exposes swagger.json at /swagger/v1/swagger.json not /{org}/{app}/swagger/v1/swagger.json
        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        var openApi = await response.Content.ReadAsStringAsync();

        ValidateOpenApiSpec(openApi);
        await VerifyOpenApiSpec(openApi);
    }

    [Fact]
    public async Task SaveCustomOpenApiSpec()
    {
        var org = "tdd";
        var app = "contributer-restriction";
        using HttpClient client = GetRootedClient(org, app);
        // The test project exposes swagger.json at /swagger/v1/swagger.json not /{org}/{app}/swagger/v1/swagger.json
        using HttpResponseMessage response = await client.GetAsync($"/{org}/{app}/v1/customOpenapi.json");

        var openApi = await response.Content.ReadAsStringAsync();
        ValidateOpenApiSpec(openApi);
        await VerifyOpenApiSpec(openApi);
    }

    private static void ValidateOpenApiSpec(string openApi)
    {
        var reader = new OpenApiStringReader();
        reader.Read(openApi, out OpenApiDiagnostic diagnostic);
        Assert.Empty(diagnostic.Errors);
    }

    private static async Task VerifyOpenApiSpec(string openApi)
    {
        await VerifyJson(openApi)
            .ScrubMember("version")
            .UseStrictJson()
            .DontScrubGuids()
            .DontIgnoreEmptyCollections()
            .AddExtraSettings(s => s.MetadataPropertyHandling = MetadataPropertyHandling.Ignore);
    }
}
