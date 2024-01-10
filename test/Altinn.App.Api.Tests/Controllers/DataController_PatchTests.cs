using Altinn.App.Api.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Altinn.App.Api.Models;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Data.apps.tdd.contributer_restriction.models;
using Altinn.App.Core.Features;
using Xunit;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Json.Patch;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class DataControllerPatchTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "tdd";
    private const string App = "contributer-restriction";
    private const int InstanceOwnerPartyId = 500600;
    private static readonly Guid InstanceGuid = new("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
    private static readonly string InstanceId = $"{InstanceOwnerPartyId}/{InstanceGuid}";
    private static readonly Guid DataGuid = new("fc121812-0336-45fb-a75c-490df3ad5109");

    private readonly Mock<IDataProcessor> _dataProcessorMock = new();
    private readonly Mock<IFormDataValidator> _formDataValidatorMock = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    };

    private readonly ITestOutputHelper _outputHelper;

    public DataControllerPatchTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper) : base(factory)
    {
        _formDataValidatorMock.Setup(v => v.DataType).Returns("Not a valid data type");
        _outputHelper = outputHelper;
        OverrideServicesForAllTests = (services) =>
        {
            services.AddSingleton(_dataProcessorMock.Object);
            services.AddSingleton(_formDataValidatorMock.Object);
        };
        TestData.DeleteInstanceAndData(Org, App, InstanceOwnerPartyId, InstanceGuid);
        TestData.PrepareInstance(Org, App, InstanceOwnerPartyId, InstanceGuid);
    }

    private async Task<HttpResponseMessage> CallPatchApi(JsonPatch patch, List<string>? ignoredValidators)
    {
        using var httpClient = GetRootedClient(Org, App);
        string token = PrincipalUtil.GetToken(1337, null);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var updateDataElementContent =
            new StringContent(
                JsonSerializer.Serialize(new DataPatchRequest()
                {
                    Patch = patch,
                    IgnoredValidators = ignoredValidators,
                }, JsonSerializerOptions), System.Text.Encoding.UTF8, "application/json");
        return await httpClient.PatchAsync($"/{Org}/{App}/instances/{InstanceId}/data/{DataGuid}", updateDataElementContent);
    }

    private async Task<string> LogResponse(HttpResponseMessage response)
    {
        var responseString = await response.Content.ReadAsStringAsync();
        using var responseParsedRaw = JsonDocument.Parse(responseString);
        _outputHelper.WriteLine(JsonSerializer.Serialize(responseParsedRaw, JsonSerializerOptions));
        return responseString;

    }
    private TResponse ParseResponse<TResponse>(string responseString)
    {
        return JsonSerializer.Deserialize<TResponse>(responseString, JsonSerializerOptions)!;
    }

    [Fact]
    public async Task PatchDataElement_ValidName_ReturnsOk()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("\"Ivar Nesje\"")));

        var response = await CallPatchApi(patch, null);
        var responseString = await LogResponse(response);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var parsedResponse = ParseResponse<DataPatchResponse>(responseString);
        parsedResponse.ValidationIssues.Should().ContainKey("required").WhoseValue.Should().BeEmpty();

        var newModelElement = parsedResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var newModel = newModelElement.Deserialize<Skjema>()!;
        newModel.Melding.Name.Should().Be("Ivar Nesje");

        _dataProcessorMock.Verify(p => p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == DataGuid), It.IsAny<Skjema>(), It.IsAny<Skjema?>()), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchDataElement_NullName_ReturnsOkAndValidationError()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Test(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")),
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")));

        var response = await CallPatchApi(patch, null);
        var responseString = await LogResponse(response);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
        var parsedResponse = ParseResponse<DataPatchResponse>(responseString);
        var requiredList = parsedResponse.ValidationIssues.Should().ContainKey("required").WhoseValue;
        var requiredName = requiredList.Should().ContainSingle().Which;
        requiredName.Field.Should().Be("melding.name");
        requiredName.Description.Should().Be("melding.name is required in component with id name");

        var newModelElement = parsedResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var newModel = newModelElement.Deserialize<Skjema>()!;
        newModel.Melding.Name.Should().BeNull();

        _dataProcessorMock.Verify(p => p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == DataGuid), It.IsAny<Skjema>(), It.IsAny<Skjema?>()), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatchDataElement_InvalidTest_ReturnsPreconditionFailed()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Test(JsonPointer.Create("melding", "name"), JsonNode.Parse("\"Not correct previous value\"")),
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")));

        var response = await CallPatchApi(patch, null);

        var responseString = await LogResponse(response);
        response.Should().HaveStatusCode(HttpStatusCode.PreconditionFailed);

        var parsedResponse = ParseResponse<ProblemDetails>(responseString);
        parsedResponse.Detail.Should().Be("Path `/melding/name` is not equal to the indicated value.");

        _dataProcessorMock.VerifyNoOtherCalls();
    }
}