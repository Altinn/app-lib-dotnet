using System.Net;
using Altinn.App.Api.Tests.Data;
using Altinn.Platform.Storage.Interface.Models;
using Argon;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Process.ServiceTasks.Pdf;

public class PdfServiceTaskTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "ttd";
    private const string App = "service-tasks";
    private const int InstanceOwnerPartyId = 501337; //Sofie Salt
    private const string Language = "nb";
    private static readonly Guid _instanceGuid = new("a2af1cfd-db99-45f9-9625-9dfa1223485f");
    private static readonly string _instanceId = $"{InstanceOwnerPartyId}/{_instanceGuid}";

    public PdfServiceTaskTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        // OverrideServicesForAllTests = [];
        TestData.DeleteInstanceAndData(Org, App, InstanceOwnerPartyId, _instanceGuid);
        TestData.PrepareInstance(Org, App, InstanceOwnerPartyId, _instanceGuid);
    }

    [Fact]
    public async Task Can_Set_PdfServiceTask_As_CurrentTask()
    {
        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next
        using HttpResponseMessage nextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );

        string nextResponseContent = await nextResponse.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(nextResponseContent);

        nextResponse.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Can_Execute_PdfServiceTask_And_Move_To_Next_Task()
    {
        var sendAsyncCalled = false;

        // Mock HttpClient for the expected pdf service call
        SendAsync = message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/pdf");
            sendAsyncCalled = true;

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("this is the binary pdf content"),
                }
            );
        };

        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next
        using HttpResponseMessage firstNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );
        firstNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        //Run process next again to actually execute the service task
        using HttpResponseMessage secondNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?lang={Language}",
            null
        );

        secondNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);
        sendAsyncCalled.Should().BeTrue();

        // Check that the process has been moved to the next task
        string nextResponseContent = await secondNextResponse.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(nextResponseContent);
        var processState = JsonConvert.DeserializeObject<ProcessState>(nextResponseContent);
        processState.CurrentTask.AltinnTaskType.Should().Be("eFormidling");
    }

    [Fact]
    public async Task Does_Not_Change_Task_When_Pdf_Fails()
    {
        var sendAsyncCalled = false;

        // Mock HttpClient for the expected pdf service call
        SendAsync = message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/pdf");
            sendAsyncCalled = true;

            //Simulate failing PDF service
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        };

        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next
        using HttpResponseMessage firstNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );
        firstNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        //Run process next again to actually execute the service task
        using HttpResponseMessage secondNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?lang={Language}",
            null
        );

        secondNextResponse.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
        sendAsyncCalled.Should().BeTrue();

        // Check that the process has been moved to the next task
        string nextResponseContent = await secondNextResponse.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(nextResponseContent);
        nextResponseContent
            .Should()
            .Be("{\"title\":\"Internal server error\",\"status\":500,\"detail\":\"Server action pdf failed!\"}");

        //Double check that process did not move to the next task
        Instance instance = await TestData.GetInstance(Org, App, InstanceOwnerPartyId, _instanceGuid);
        instance.Process.CurrentTask.ElementId.Should().Be("Task_2");
        instance.Process.CurrentTask.AltinnTaskType.Should().Be("pdf");
    }
}
