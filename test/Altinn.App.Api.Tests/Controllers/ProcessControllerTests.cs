using System.Net;
using System.Net.Http.Headers;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Altinn.App.Api.Tests.Controllers
{
    public class ProcessControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public ProcessControllerTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Get_ShouldReturnProcessTasks()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            int partyId = 500000;
            Guid instanceId = new Guid("5d9e906b-83ed-44df-85a7-2f104c640bff");
            HttpClient client = GetRootedClient(org, app);

            TestData.DeleteInstance(org, app, partyId, instanceId);
            TestData.PrepareInstance(org, app, partyId, instanceId);

            string token = PrincipalUtil.GetToken(1337, 500000, 3);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/{org}/{app}/instances/{partyId}/{instanceId}/process";
            HttpResponseMessage response = await client.GetAsync(url);
            TestData.DeleteInstance(org, app, partyId, instanceId);
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().BeEquivalentTo(""" 
                {
                  "currentTask": {
                    "actions": {},
                    "read": true,
                    "write": true,
                    "flow": 2,
                    "started": "2023-11-23T13:36:56.678338Z",
                    "elementId": "Task_1",
                    "name": "Utfylling (message)",
                    "altinnTaskType": "data",
                    "ended": null,
                    "validated": null,
                    "flowType": "CompleteCurrentMoveToNext"
                  },
                  "processTasks": [
                    {
                      "altinnTaskType": "data",
                      "elementId": "Task_1"
                    },
                    {
                      "altinnTaskType": "data",
                      "elementId": "Task_2"
                    },
                    {
                      "altinnTaskType": "data",
                      "elementId": "Task_3"
                    },
                    {
                      "altinnTaskType": "data",
                      "elementId": "Task_4"
                    },
                    {
                      "altinnTaskType": "data",
                      "elementId": "Task_5"
                    },
                    {
                      "altinnTaskType": "confirmation",
                      "elementId": "Task_6"
                    }
                  ],
                  "started": "2023-11-23T13:36:56.6782592Z",
                  "startEvent": "StartEvent_1",
                  "ended": null,
                  "endEvent": null
                }
            """);
        }
    }
}