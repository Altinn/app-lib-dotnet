﻿using System.Net;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Argon;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Process.ServiceTasks.EFormidling;

public class EFormidlingServiceTaskTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "ttd";
    private const string App = "service-tasks";
    private const int InstanceOwnerPartyId = 501337; //Sofie Salt
    private const string Language = "nb";
    private static readonly Guid _instanceGuid = new("b1af1cfd-db99-45f9-9625-9dfa1223485f");
    private static readonly string _instanceId = $"{InstanceOwnerPartyId}/{_instanceGuid}";

    private readonly Mock<IEFormidlingService> _eFormidlingServiceMock = new Mock<IEFormidlingService>();

    public EFormidlingServiceTaskTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        OverrideServicesForAllTests = (services) =>
        {
            services.AddSingleton(_eFormidlingServiceMock.Object);
        };

        TestData.DeleteInstanceAndData(Org, App, InstanceOwnerPartyId, _instanceGuid);
        TestData.PrepareInstance(Org, App, InstanceOwnerPartyId, _instanceGuid);
    }

    [Fact]
    public async Task Can_Set_EFormidlingServiceTask_As_CurrentTask()
    {
        SendAsync = message =>
        {
            if (message.RequestUri!.PathAndQuery.Contains("pdf"))
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("this is the binary pdf content"),
                    }
                );
            }

            throw new Exception($"Not mocked http request: {message.RequestUri!.PathAndQuery}");
        };

        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next
        HttpResponseMessage nextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );

        string nextResponseContent = await nextResponse.Content.ReadAsStringAsync();
        _outputHelper.WriteLine(nextResponseContent);

        nextResponse.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Can_Execute_EFormidlingServiceTask_And_Move_To_Next_Task()
    {
        // Make sure a request to eFormidling is made
        SendAsync = message =>
        {
            if (message.RequestUri!.PathAndQuery.Contains("pdf"))
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("this is the binary pdf content"),
                    }
                );
            }

            throw new Exception($"Not mocked http request: {message.RequestUri!.PathAndQuery}");
        };

        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next to move from PdfServiceTask to EFormidlingServiceTask
        HttpResponseMessage firstNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );
        firstNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        //Run process next again to execute the EFormidlingServiceTask
        HttpResponseMessage secondNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?lang={Language}",
            null
        );

        secondNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        // Check that the process has been moved to end task
        string nextResponseContent = await secondNextResponse.Content.ReadAsStringAsync();
        _outputHelper.WriteLine(nextResponseContent);
        var processState = JsonConvert.DeserializeObject<ProcessState>(nextResponseContent);
        processState.Ended.Should().NotBeNull();
    }

    [Fact]
    public async Task Does_Not_Change_Task_When_EFormidling_Fails()
    {
        // Make sure a request to eFormidling is made
        SendAsync = message =>
        {
            if (message.RequestUri!.PathAndQuery.Contains("pdf"))
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("this is the binary pdf content"),
                    }
                );
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        };

        using HttpClient client = GetRootedClient(Org, App, 1337, InstanceOwnerPartyId);

        //Run process next to move from PdfServiceTask to EFormidlingServiceTask
        HttpResponseMessage firstNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?language={Language}",
            null
        );
        firstNextResponse.Should().HaveStatusCode(HttpStatusCode.OK);

        //Setup eFormidling service to throw exception
        _eFormidlingServiceMock
            .Setup(x => x.SendEFormidlingShipment(It.IsAny<Instance>()))
            .ThrowsAsync(new Exception());

        //Run process next again to execute the EFormidlingServiceTask
        HttpResponseMessage secondNextResponse = await client.PutAsync(
            $"{Org}/{App}/instances/{_instanceId}/process/next?lang={Language}",
            null
        );

        secondNextResponse.Should().HaveStatusCode(HttpStatusCode.InternalServerError);

        // Check that the process has been moved to end task
        Instance instance = await TestData.GetInstance(Org, App, InstanceOwnerPartyId, _instanceGuid);
        instance.Process.CurrentTask.ElementId.Should().Be("Task_3");
        instance.Process.CurrentTask.AltinnTaskType.Should().Be("eFormidling");
    }
}
