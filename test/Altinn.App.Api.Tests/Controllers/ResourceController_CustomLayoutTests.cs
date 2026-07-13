using System.Net;
using System.Text.Json;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Internal.App;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class ResourceController_CustomLayoutTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ResourceController_CustomLayoutTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    private class CustomLayoutForInstance : ICustomLayoutForInstance
    {
        public Task<string?> GetCustomLayoutForInstance(string layoutSetId, int instanceOwnerPartyId, Guid instanceId)
        {
            return Task.FromResult<string?>(instanceId.ToString());
        }

        public Task<string?> GetCustomLayoutSettingsForInstance(
            string layoutSetId,
            int instanceOwnerPartyId,
            Guid instanceId
        )
        {
            return Task.FromResult<string?>(instanceId.ToString());
        }
    }

    [Fact]
    public async Task GetLayoutsForSet_WithCustomLayoutForInstanceService_ReturnsOk()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton<ICustomLayoutForInstance, CustomLayoutForInstance>();
        };

        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 500600;
        Guid instanceGuid = Guid.Parse("cff1cb24-5bc1-4888-8e06-c634753c5144");
        string layoutSetId = "default";
        using HttpClient client = GetRootedUserClient(org, app, 1337, instanceOwnerPartyId);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);
        var response = await client.GetAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/layouts/{layoutSetId}"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(instanceGuid.ToString(), content);
    }

    [Fact]
    public async Task GetLayoutsForSet_WithoutCustomLayoutForInstanceService_ReturnsOk()
    {
        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 500600;
        Guid instanceGuid = Guid.Parse("cff1cb24-5bc1-4888-8e06-c634753c5144");
        string layoutSetId = "default";
        using HttpClient client = GetRootedUserClient(org, app, 1337, instanceOwnerPartyId);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);
        var response = await client.GetAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/layouts/{layoutSetId}"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("page", out var pageLayout));
        Assert.True(pageLayout.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("layout", out var layout));
        Assert.Equal(JsonValueKind.Array, layout.ValueKind);
        Assert.True(layout.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetLayoutSettingsForSet_WithCustomLayoutForInstanceService_ReturnsOk()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton<ICustomLayoutForInstance, CustomLayoutForInstance>();
        };

        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 500600;
        Guid instanceGuid = Guid.Parse("cff1cb24-5bc1-4888-8e06-c634753c5144");
        string layoutSetId = "default";
        using HttpClient client = GetRootedUserClient(org, app, 1337, instanceOwnerPartyId);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);
        var response = await client.GetAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/layoutsettings/{layoutSetId}"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(instanceGuid.ToString(), content);
    }

    [Fact]
    public async Task GetLayoutSettingsForSet_WithoutCustomLayoutForInstanceService_ReturnsOk()
    {
        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 500600;
        Guid instanceGuid = Guid.Parse("cff1cb24-5bc1-4888-8e06-c634753c5144");
        string layoutSetId = "default";
        using HttpClient client = GetRootedUserClient(org, app, 1337, instanceOwnerPartyId);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);
        var response = await client.GetAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/layoutsettings/{layoutSetId}"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
    }
}
