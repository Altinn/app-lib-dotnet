using System.Net;
using System.Text.Json;
using Altinn.App.Api.Tests.Controllers;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Data.apps.tdd.contributer_restriction.models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Process.ServiceTasks;

public class ServiceTaskFirstTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "ttd";
    private const string App = "service-task-first";
    private const int InstanceOwnerPartyId = 501337;

    private bool _serviceTaskExecuted;

    public ServiceTaskFirstTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        OverrideServicesForAllTests = services =>
        {
            services.AddTransient<IServiceTask>(sp =>
            {
                var task = new CustomServiceTask(() => _serviceTaskExecuted = true);
                return task;
            });
        };

        SendAsync = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Fact]
    public async Task Instantiate_AutoRuns_ServiceTask_And_Lands_On_UserTask()
    {
        using HttpClient client = GetRootedUserClient(Org, App);

        var (instance, _) = await InstancesControllerFixture.CreateInstanceSimplified(
            Org,
            App,
            InstanceOwnerPartyId,
            client,
            TestAuthentication.GetUserToken(userId: 1337, partyId: InstanceOwnerPartyId)
        );

        _serviceTaskExecuted.Should().BeTrue("the service task should have been auto-executed on instantiation");

        instance.Process.Should().NotBeNull();
        instance.Process!.CurrentTask.Should().NotBeNull();
        instance
            .Process.CurrentTask!.ElementId.Should()
            .Be("Task_1", "process should have advanced past the service task");
        instance.Process.CurrentTask.AltinnTaskType.Should().Be("data");

        instance.Data.Should().HaveCount(1, "auto-created data element for the user task");

        TestData.DeleteInstanceAndData(Org, App, instance.Id);
    }

    [Fact]
    public async Task Instantiate_With_Prefill_Reaches_UserTask_DataModel()
    {
        using HttpClient client = GetRootedUserClient(Org, App);

        var prefill = new Dictionary<string, string> { { "melding.name", "PrefillThroughServiceTask" } };
        var (instance, _) = await InstancesControllerFixture.CreateInstanceSimplified(
            Org,
            App,
            InstanceOwnerPartyId,
            client,
            TestAuthentication.GetUserToken(userId: 1337, partyId: InstanceOwnerPartyId),
            prefill
        );

        _serviceTaskExecuted.Should().BeTrue("the service task should have been auto-executed on instantiation");

        instance.Process!.CurrentTask!.ElementId.Should().Be("Task_1");
        instance.Data.Should().HaveCount(1);

        var dataGuid = instance.Data.First().Id;
        var readResponse = await client.GetAsync($"/{Org}/{App}/instances/{instance.Id}/data/{dataGuid}");
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await readResponse.Content.ReadAsStringAsync();
        var model = JsonSerializer.Deserialize<Skjema>(content);

        model.Should().NotBeNull();
        model!.Melding.Should().NotBeNull("prefill should have been applied to the user task data model");
        model.Melding!.Name.Should().Be("PrefillThroughServiceTask");

        TestData.DeleteInstanceAndData(Org, App, instance.Id);
    }

    private sealed class CustomServiceTask : IServiceTask
    {
        private readonly Action _onExecute;

        public CustomServiceTask(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public string Type => "custom-service";

        public Task<ServiceTaskResult> Execute(ServiceTaskContext context)
        {
            _onExecute();
            return Task.FromResult<ServiceTaskResult>(ServiceTaskResult.Success());
        }
    }
}
