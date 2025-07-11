using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Altinn.App.Api.Models;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Json.Patch;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class DataController_RequiredActionTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private readonly Mock<IDataProcessor> _dataProcessor = new();
    const string OrgId = "tdd";
    const string AppId = "contributer-restriction";

    public DataController_RequiredActionTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        OverrideServicesForAllTests = (services) =>
        {
            services.AddSingleton(_dataProcessor.Object);
        };
    }

    [Theory]
    [InlineData("userInteractionUnspecified", false, HttpStatusCode.OK)]
    [InlineData("userInteractionUnspecified", true, HttpStatusCode.OK)]
    [InlineData("requiresActionToRead", false, HttpStatusCode.Forbidden)]
    [InlineData("requiresActionToRead", true, HttpStatusCode.OK)]
    public async Task ReadDataElement_ImplementsAndValidates_ActionRequiredToReadProperty(
        string dataModelId,
        bool instantiateAsOrg,
        HttpStatusCode expectedStatusCode
    )
    {
        // Arrange
        using var instance = await CreateAppInstance(instantiateAsOrg);

        /* Create a datamodel so we have something to delete */
        using var systemClient = GetRootedOrgClient(OrgId, AppId, serviceOwnerOrg: OrgId);
        var createResponse = await systemClient.PostAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data?dataType={dataModelId}",
            null
        );
        var createResponseParsed = await VerifyStatusAndDeserialize<DataElement>(
            createResponse,
            HttpStatusCode.Created
        );

        // Act
        var response = await instance.AuthenticatedClient.GetAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data/{createResponseParsed.Id}"
        );

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        TestData.DeleteInstanceAndData(OrgId, AppId, instance.Id);
    }

    [Theory]
    [InlineData("userInteractionUnspecified", false, HttpStatusCode.Created)]
    [InlineData("userInteractionUnspecified", true, HttpStatusCode.Created)]
    [InlineData("requiresActionToWrite", false, HttpStatusCode.Forbidden)]
    [InlineData("requiresActionToWrite", true, HttpStatusCode.Created)]
    public async Task CreateDataElement_ImplementsAndValidates_ActionRequiredToWriteProperty(
        string dataModelId,
        bool instantiateAsOrg,
        HttpStatusCode expectedStatusCode
    )
    {
        // Arrange
        using var instance = await CreateAppInstance(instantiateAsOrg);

        // Act
        var response = await instance.AuthenticatedClient.PostAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data?dataType={dataModelId}",
            null
        );

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        TestData.DeleteInstanceAndData(OrgId, AppId, instance.Id);
    }

    [Theory]
    [InlineData("userInteractionUnspecified", false, HttpStatusCode.Created)]
    [InlineData("userInteractionUnspecified", true, HttpStatusCode.Created)]
    [InlineData("requiresActionToWrite", false, HttpStatusCode.Forbidden)]
    [InlineData("requiresActionToWrite", true, HttpStatusCode.Created)]
    public async Task PutDataElement_ImplementsAndValidates_ActionRequiredToWriteProperty(
        string dataModelId,
        bool instantiateAsOrg,
        HttpStatusCode expectedStatusCode
    )
    {
        // Arrange
        using var instance = await CreateAppInstance(instantiateAsOrg);

        /* Create a datamodel so we have something to delete */
        using var systemClient = GetRootedOrgClient(OrgId, AppId, serviceOwnerOrg: OrgId);
        var createResponse = await systemClient.PostAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data?dataType={dataModelId}",
            null
        );
        var createResponseParsed = await VerifyStatusAndDeserialize<DataElement>(
            createResponse,
            HttpStatusCode.Created
        );
        using var updateDataElementContent = new StringContent(
            """{"melding":{"name": "Ola Olsen"}}""",
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await instance.AuthenticatedClient.PutAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data/{createResponseParsed.Id}",
            updateDataElementContent
        );

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        TestData.DeleteInstanceAndData(OrgId, AppId, instance.Id);
    }

    [Theory]
    [InlineData("userInteractionUnspecified", false, HttpStatusCode.OK)]
    [InlineData("userInteractionUnspecified", true, HttpStatusCode.OK)]
    [InlineData("requiresActionToWrite", false, HttpStatusCode.Forbidden)]
    [InlineData("requiresActionToWrite", true, HttpStatusCode.OK)]
    public async Task PatchDataElement_ImplementsAndValidates_ActionRequiredToWriteProperty(
        string dataModelId,
        bool instantiateAsOrg,
        HttpStatusCode expectedStatusCode
    )
    {
        // Arrange
        using var instance = await CreateAppInstance(instantiateAsOrg);

        /* Create a datamodel so we have something to delete */
        using var systemClient = GetRootedOrgClient(OrgId, AppId, serviceOwnerOrg: OrgId);
        var createResponse = await systemClient.PostAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data?dataType={dataModelId}",
            new StringContent("""{"melding":{"name": "Ola Olsen"}}""", Encoding.UTF8, "application/json")
        );
        var createResponseParsed = await VerifyStatusAndDeserialize<DataElement>(
            createResponse,
            HttpStatusCode.Created
        );

        var pointer = JsonPointer.Create("melding", "name");
        var patch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("\"Ola Olsen\"")),
            PatchOperation.Replace(pointer, JsonNode.Parse("\"Olga Olsen\""))
        );
        var serializedPatch = JsonSerializer.Serialize(
            new DataPatchRequest() { Patch = patch },
            DataControllerPatchTests._jsonSerializerOptions
        );
        using var updateDataElementContent = new StringContent(serializedPatch, Encoding.UTF8, "application/json");

        // Act
        var response = await instance.AuthenticatedClient.PatchAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data/{createResponseParsed.Id}",
            updateDataElementContent
        );

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        TestData.DeleteInstanceAndData(OrgId, AppId, instance.Id);
    }

    [Theory]
    [InlineData("userInteractionUnspecified", false, HttpStatusCode.OK)]
    [InlineData("userInteractionUnspecified", true, HttpStatusCode.OK)]
    [InlineData("requiresActionToWrite", false, HttpStatusCode.Forbidden)]
    [InlineData("requiresActionToWrite", true, HttpStatusCode.OK)]
    public async Task DeleteDataElement_ImplementsAndValidates_ActionRequiredToWriteProperty(
        string dataModelId,
        bool instantiateAsOrg,
        HttpStatusCode expectedStatusCode
    )
    {
        // Arrange
        using var instance = await CreateAppInstance(instantiateAsOrg);

        /* Create a datamodel so we have something to delete */
        using var systemClient = GetRootedOrgClient(OrgId, AppId, serviceOwnerOrg: OrgId);
        var createResponse = await systemClient.PostAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data?dataType={dataModelId}",
            null
        );
        var createResponseParsed = await VerifyStatusAndDeserialize<DataElement>(
            createResponse,
            HttpStatusCode.Created
        );

        // Act
        var response = await instance.AuthenticatedClient.DeleteAsync(
            $"/{instance.Org}/{instance.App}/instances/{instance.Id}/data/{createResponseParsed.Id}"
        );

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);

        TestData.DeleteInstanceAndData(OrgId, AppId, instance.Id);
    }

    private async Task<AppInstance> CreateAppInstance(bool actAsOrg)
    {
        var instanceOwnerPartyId = 501337;
        var userId = 1337;
        HttpClient client = actAsOrg
            ? GetRootedOrgClient(OrgId, AppId, serviceOwnerOrg: OrgId)
            : GetRootedUserClient(OrgId, AppId, userId, instanceOwnerPartyId);

        var response = await client.PostAsync(
            $"{OrgId}/{AppId}/instances/?instanceOwnerPartyId={instanceOwnerPartyId}",
            null
        );
        var createResponseParsed = await VerifyStatusAndDeserialize<Instance>(response, HttpStatusCode.Created);

        return new AppInstance(createResponseParsed.Id, OrgId, AppId, client);
    }

    private record AppInstance(string Id, string Org, string App, HttpClient AuthenticatedClient) : IDisposable
    {
        public void Dispose() => AuthenticatedClient.Dispose();
    }
}
