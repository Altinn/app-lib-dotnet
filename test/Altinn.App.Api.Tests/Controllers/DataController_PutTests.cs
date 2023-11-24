using Altinn.App.Api.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net;
using Altinn.App.Api.Tests.Data.apps.tdd.contributer_restriction.models;
using Altinn.App.Core.Features;
using Xunit;
using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Api.Tests.Controllers;

public class DataController_PutTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IDataProcessor> _dataProcessor = new();
    public DataController_PutTests(WebApplicationFactory<Program> factory) : base(factory)
    {
        OverrideServicesForAllTests = (services) =>
        {
            services.AddSingleton<IDataProcessor>(_dataProcessor.Object);
        };
    }

    [Fact]
    public async Task PutDataElement_TestSinglePartUpdate_ReturnsOk()
    {
        // Setup test data
        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 501337;
        HttpClient client = GetRootedClient(org, app);
        string token = PrincipalUtil.GetToken(1337, 4);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create instance
        var createResponse =
            await client.PostAsync($"{org}/{app}/instances/?instanceOwnerPartyId={instanceOwnerPartyId}", null);
        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseParsed = JsonSerializerPermissive.Deserialize<Instance>(createResponseContent);
        var instanceId = createResponseParsed.Id;

        // Create data element (not sure why it isn't created when the instance is created, autoCreate is true)
        using var createDataElementContent =
            new StringContent("""{"melding":{"name": "Ivar"}}""", System.Text.Encoding.UTF8, "application/json");
        var createDataElementResponse =
            await client.PostAsync($"/{org}/{app}/instances/{instanceId}/data?dataType=default", createDataElementContent);
        var createDataElementResponseContent = await createDataElementResponse.Content.ReadAsStringAsync();
        createDataElementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createDataElementResponseParsed =
            JsonSerializerPermissive.Deserialize<DataElement>(createDataElementResponseContent);
        var dataGuid = createDataElementResponseParsed.Id;
        
        // Update data element
        using var updateDataElementContent =
            new StringContent("""{"melding":{"name": "Ivar Nesje"}}""", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/{org}/{app}/instances/{instanceId}/data/{dataGuid}", updateDataElementContent);
        
        // Verify stored data
        var readDataElementResponse = await client.GetAsync($"/{org}/{app}/instances/{instanceId}/data/{dataGuid}");
        var readDataElementResponseContent = await readDataElementResponse.Content.ReadAsStringAsync();
        var readDataElementResponseParsed =
            JsonSerializerPermissive.Deserialize<Skjema>(readDataElementResponseContent);
        readDataElementResponseParsed.Melding.Name.Should().Be("Ivar Nesje");
        
        _dataProcessor.Verify(p=>p.ProcessDataRead(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == Guid.Parse(dataGuid)), It.IsAny<Skjema>()), Times.Exactly(1));
        _dataProcessor.Verify(p=>p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == Guid.Parse(dataGuid)), It.IsAny<Skjema>()), Times.Exactly(1)); // TODO: Shouldn't this be 2 because of the first write?
        _dataProcessor.VerifyNoOtherCalls();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}