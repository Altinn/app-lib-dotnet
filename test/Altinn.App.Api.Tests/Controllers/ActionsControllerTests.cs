using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.App.Api.Models;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Utils;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class ActionsControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(
        JsonSerializerDefaults.Web
    );
    private readonly ITestOutputHelper _outputHelper;

    public ActionsControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Perform_returns_403_if_user_not_authorized()
    {
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent(
            "{\"action\":\"lookup_unauthorized\"}",
            Encoding.UTF8,
            "application/json"
        );
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
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
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        using var content = new StringContent(
            "{\"action\":\"lookup_unauthorized\"}",
            Encoding.UTF8,
            "application/json"
        );
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
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
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(null, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent(
            "{\"action\":\"lookup_unauthorized\"}",
            Encoding.UTF8,
            "application/json"
        );
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
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
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":null}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
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
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
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
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Perform_returns_200_if_action_succeeded()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1000, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var requestContent = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            requestContent
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var expectedString = """
            {
              "instance": {},
              "updatedDataModels": {},
              "updatedValidationIssues": {},
              "clientActions": [
                {
                  "id": "nextPage",
                  "metadata": null
                }
              ],
              "error": null
            }
            """;
        CompareResult<UserActionResponse>(
            expectedString,
            content,
            mutator: actionResponse =>
            {
                // Don't compare the instance object
                if (actionResponse != null)
                {
                    actionResponse.Instance = new();
                }
            }
        );
    }

    [Fact]
    public async Task Perform_returns_400_if_action_failed_and_errorType_is_BadRequest()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(400, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Perform_returns_401_if_action_failed_and_errorType_is_Unauthorized()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(401, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Perform_returns_409_if_action_failed_and_errorType_is_Conflict()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(409, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Perform_returns_500_if_action_failed_and_errorType_is_Internal()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(500, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"lookup\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Perform_returns_404_if_action_implementation_not_found()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddTransient<IUserAction, LookupAction>();
        };
        var org = "tdd";
        var app = "task-action";
        HttpClient client = GetRootedClient(org, app);
        Guid guid = new Guid("b1135209-628e-4a6e-9efd-e4282068ef41");
        TestData.DeleteInstanceAndData(org, app, 1337, guid);
        TestData.PrepareInstance(org, app, 1337, guid);
        string token = PrincipalUtil.GetToken(1001, null, 3);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var content = new StringContent("{\"action\":\"notfound\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(
            $"/{org}/{app}/instances/1337/{guid}/actions",
            content
        );
        // Cleanup testdata
        TestData.DeleteInstanceAndData(org, app, 1337, guid);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    //TODO: replace this assertion with a proper one once fluentassertions has a json compare feature scheduled for v7 https://github.com/fluentassertions/fluentassertions/issues/2205
    private void CompareResult<T>(string expectedString, string actualString, Action<T?>? mutator = null)
    {
        _outputHelper.WriteLine($"Expected: {expectedString}");
        _outputHelper.WriteLine($"Actual: {actualString}");
        T? expected = JsonSerializer.Deserialize<T>(expectedString, _jsonSerializerOptions);
        T? actual = JsonSerializer.Deserialize<T>(actualString, _jsonSerializerOptions);
        mutator?.Invoke(actual);
        mutator?.Invoke(expected);
        actual.Should().BeEquivalentTo(expected);
    }
}

public class LookupAction : IUserAction
{
    public string Id => "lookup";

    public async Task<UserActionResult> HandleAction(UserActionContext context)
    {
        await Task.CompletedTask;
        if (context.UserId == 400)
        {
            return UserActionResult.FailureResult(new ActionError(), errorType: ProcessErrorType.BadRequest);
        }

        if (context.UserId == 401)
        {
            return UserActionResult.FailureResult(new ActionError(), errorType: ProcessErrorType.Unauthorized);
        }

        if (context.UserId == 409)
        {
            return UserActionResult.FailureResult(new ActionError(), errorType: ProcessErrorType.Conflict);
        }

        if (context.UserId == 500)
        {
            return UserActionResult.FailureResult(new ActionError(), errorType: ProcessErrorType.Internal);
        }
        return UserActionResult.SuccessResult(new List<ClientAction>() { ClientAction.NextPage() });
    }
}
