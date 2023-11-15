using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Altinn.App.Api.Tests.Controllers;

public class ActionsControllerTests: ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public ActionsControllerTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }
    
    [Fact]
    public async Task Perform_returns_403_if_user_not_authorized()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":\"lookup_unauthorized\"}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task Perform_returns_401_if_user_not_authenticated()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":\"lookup_unauthorized\"}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Perform_returns_401_if_userId_is_null()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":\"lookup_unauthorized\"}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Perform_returns_400_if_action_is_null()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":null}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Perform_returns_409_if_process_not_started()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef43");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    
    [Fact]
    public async Task Perform_returns_409_if_process_ended()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef42");
        TestData.DeleteInstance(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.PostAsync($"/{org}/{app}/instances/1337/{guid}/actions", new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json"));
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}