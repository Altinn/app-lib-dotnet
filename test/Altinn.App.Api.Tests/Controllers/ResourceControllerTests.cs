using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class ResourceControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ResourceControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    private const string Org = "tdd";
    private const string App = "contributer-restriction";
    private const string DefaultDataTypeId = "Skjema";

    [Fact]
    public async Task GetModelJsonSchema_ReturnsOk()
    {
        var client = GetRootedClient(Org, App);
        using var response = await client.GetAsync($"/{Org}/{App}/api/jsonschema/{DefaultDataTypeId}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.Contains("\"$comment\"", content); // TODO: Update when the schema in the test project has more content.
    }

    [Fact]
    public async Task GetModelJsonSchema_InvalidDataTypeId_ReturnsNotFound()
    {
        var client = GetRootedClient(Org, App);
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            using var response = await client.GetAsync($"/{Org}/{App}/api/jsonschema/InvalidDataTypeId");
            // TODO: Should probably return 404 NotFound instead of throwing FileNotFoundException,
            // but adding test for current behaviour.
            // Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    [Fact]
    public async Task GetXmlSchema_ReturnsOk()
    {
        var client = GetRootedClient(Org, App);
        using var response = await client.GetAsync($"/{Org}/{App}/api/xsdschema/{DefaultDataTypeId}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        Assert.Contains("<xs:schema xmlns:xs=", content); // TODO: Update when the schema in the test project has more content.
    }

    [Fact]
    public async Task GetXmlSchema_InvalidDataTypeId_ReturnsNotFound()
    {
        var client = GetRootedClient(Org, App);
        using var response = await client.GetAsync($"/{Org}/{App}/api/xsdschema/InvalidDataTypeId");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
