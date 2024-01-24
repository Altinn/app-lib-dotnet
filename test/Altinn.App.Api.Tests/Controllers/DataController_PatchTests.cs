using Altinn.App.Api.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Encodings.Web;
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
using Json.More;
using Json.Patch;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class DataControllerPatchTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    // Define constants
    private const string Org = "tdd";
    private const string App = "contributer-restriction";
    private const int InstanceOwnerPartyId = 500600;
    private static readonly Guid InstanceGuid = new("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
    private static readonly string InstanceId = $"{InstanceOwnerPartyId}/{InstanceGuid}";
    private static readonly Guid DataGuid = new("fc121812-0336-45fb-a75c-490df3ad5109");

    // Define mocks
    private readonly Mock<IDataProcessor> _dataProcessorMock = new();
    private readonly Mock<IFormDataValidator> _formDataValidatorMock = new();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ITestOutputHelper _outputHelper;

    // Constructor with common setup
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

    // Helper method to call the API
    private async Task<(HttpResponseMessage response, string responseString, TResponse parsedResponse)> CallPatchApi<TResponse>(JsonPatch patch, List<string>? ignoredValidators, HttpStatusCode expectedStatus, string? language = null)
    {
        var url = $"/{Org}/{App}/instances/{InstanceId}/data/{DataGuid}";
        if (language is not null)
        {
            url += $"?language={language}";
        }
        _outputHelper.WriteLine($"Calling PATCH {url}");
        using var httpClient = GetRootedClient(Org, App);
        string token = PrincipalUtil.GetToken(1337, null);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var serializedPatch = JsonSerializer.Serialize(new DataPatchRequest()
        {
            Patch = patch,
            IgnoredValidators = ignoredValidators,
        }, JsonSerializerOptions);
        _outputHelper.WriteLine(serializedPatch);
        using var updateDataElementContent =
            new StringContent(serializedPatch, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PatchAsync(url, updateDataElementContent);
        var responseString = await response.Content.ReadAsStringAsync();
        using var responseParsedRaw = JsonDocument.Parse(responseString);
        _outputHelper.WriteLine("\nResponse:");
        _outputHelper.WriteLine(JsonSerializer.Serialize(responseParsedRaw, JsonSerializerOptions));
        response.Should().HaveStatusCode(expectedStatus);
        var responseObject = JsonSerializer.Deserialize<TResponse>(responseString, JsonSerializerOptions)!;
        return (response, responseString, responseObject);
    }


    [Fact]
    public async Task ValidName_ReturnsOk()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("\"Ola Olsen\"")));

        var (_, _, parsedResponse) = await CallPatchApi<DataPatchResponse>(patch, null, HttpStatusCode.OK);

        parsedResponse.ValidationIssues.Should().ContainKey("Required").WhoseValue.Should().BeEmpty();

        var newModelElement = parsedResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var newModel = newModelElement.Deserialize<Skjema>()!;
        newModel.Melding.Name.Should().Be("Ola Olsen");

        _dataProcessorMock.Verify(p => p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == DataGuid), It.IsAny<Skjema>(), It.IsAny<Skjema?>(), null), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NullName_ReturnsOkAndValidationError()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Test(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")),
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")));

        var (_, _, parsedResponse) = await CallPatchApi<DataPatchResponse>(patch, null, HttpStatusCode.OK);

        var requiredList = parsedResponse.ValidationIssues.Should().ContainKey("Required").WhoseValue;
        var requiredName = requiredList.Should().ContainSingle().Which;
        requiredName.Field.Should().Be("melding.name");
        requiredName.Description.Should().Be("melding.name is required in component with id name");

        var newModelElement = parsedResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var newModel = newModelElement.Deserialize<Skjema>()!;
        newModel.Melding.Name.Should().BeNull();

        _dataProcessorMock.Verify(p => p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == DataGuid), It.IsAny<Skjema>(), It.IsAny<Skjema?>(), null), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidTestValue_ReturnsPreconditionFailed()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Test(JsonPointer.Create("melding", "name"), JsonNode.Parse("\"Not correct previous value\"")),
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")));

        var (_, _, parsedResponse) = await CallPatchApi<ProblemDetails>(patch, null, HttpStatusCode.PreconditionFailed);

        parsedResponse.Detail.Should().Be("Path `/melding/name` is not equal to the indicated value.");

        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidTestPath_ReturnsPreconditionFailed()
    {
        // Update data element
        var patch = new JsonPatch(
            PatchOperation.Test(JsonPointer.Create("melding", "name-error"), JsonNode.Parse("null")),
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("null")));

        var (_, _, parsedResponse) = await CallPatchApi<ProblemDetails>(patch, null, HttpStatusCode.UnprocessableContent);

        parsedResponse.Detail.Should().Be("Path `/melding/name-error` could not be reached.");

        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidJsonPointer_ReturnsUnprocessableContent()
    {
        // Update data element
        var pointer = JsonPointer.Create("not", "a pointer");
        var patch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("null")),
            PatchOperation.Replace(pointer, JsonNode.Parse("\"Ivar\"")));

        var (_, _, parsedResponse) = await CallPatchApi<ProblemDetails>(patch, null, HttpStatusCode.UnprocessableContent);

        parsedResponse.Detail.Should().Be("Path `/not/a pointer` could not be reached.");

        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestEmptyListAndInsertElement_ReturnsNewModel()
    {
        // Update data element
        var pointer = JsonPointer.Create("melding", "nested_list");
        var patch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("""[]""")),
            PatchOperation.Add(pointer, JsonNode.Parse("""[{"key": "newKey"}]""")));

        var (_, _, parsedResponse) = await CallPatchApi<DataPatchResponse>(patch, null, HttpStatusCode.OK);

        var newModel = parsedResponse.NewDataModel.Should().BeOfType<JsonElement>().Which.Deserialize<Skjema>()!;
        var listItem = newModel.Melding.NestedList.Should().ContainSingle().Which;
        listItem.Key.Should().Be("newKey");

        parsedResponse.ValidationIssues
            .Should().ContainKey("Required").WhoseValue
            .Should().Contain(i => i.Field == "melding.name");

        _dataProcessorMock.Verify(
            p => p.ProcessDataWrite(
                It.IsAny<Instance>(),
                It.Is<Guid>(dataId => dataId == DataGuid),
                It.Is<Skjema>(s=>s.Melding.NestedList.Count == 1),
                It.Is<Skjema?>(s => s!.Melding.NestedList.Count == 0),
                null
                ), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddItemToNonInitializedList_ReturnsUnprocessableEntity()
    {
        // This test fails to initialize the list, thus creating an error
        // Added this test to ensure that a change in behaviour (when changing json patch library)
        // is detected
        var pointer = JsonPointer.Create("melding", "nested_list", 0, "newKey");
        var patch = new JsonPatch(
            PatchOperation.Add(pointer, JsonNode.Parse("\"newValue\"")));

        var (_, _, parsedResponse) = await CallPatchApi<ProblemDetails>(patch, null, HttpStatusCode.UnprocessableContent);

        parsedResponse.Detail.Should().Contain("/melding/nested_list/0/newKey");

        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InsertNonExistingFieldWithoutTest_ReturnsUnprocessableContent()
    {
        // Update data element
        var pointer = JsonPointer.Create("melding", "non_existing_field");
        var patch = new JsonPatch(
            PatchOperation.Add(pointer, JsonNode.Parse("""[{"key": "newKey"}]""")));

        var (_, _, parsedResponse) = await CallPatchApi<ProblemDetails>(patch, null, HttpStatusCode.UnprocessableContent);

        parsedResponse.Detail.Should().Contain("The JSON property 'non_existing_field' could not be mapped to any .NET member contained in type");

        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateContainerWithListProperty_ReturnsCorrectDataModel()
    {
        var pointer = JsonPointer.Create("melding", "nested_list");
        var createFirstElementPatch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("[]")),
            PatchOperation.Add(pointer.Combine("-"), JsonNode.Parse("""{"key": "myKey" }"""))
        );

        var (_, _, firstResponse) = await CallPatchApi<DataPatchResponse>(createFirstElementPatch, null, HttpStatusCode.OK);

        var firstData = firstResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var firstListItem = firstData.GetProperty("melding").GetProperty("nested_list").EnumerateArray().First();
        firstListItem.GetProperty("values").GetArrayLength().Should().Be(0);

        var addValuePatch = new JsonPatch(
            PatchOperation.Test(pointer.Combine("0"), firstListItem.AsNode()),
            PatchOperation.Remove(pointer.Combine("0")));
        var (_, _, secondResponse) = await CallPatchApi<DataPatchResponse>(addValuePatch, null, HttpStatusCode.OK);
        var secondData = secondResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        secondData.GetProperty("melding").GetProperty("nested_list").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task RemoveStringProperty_ReturnsCorrectDataModel()
    {
        var pointer = JsonPointer.Create("melding", "name");
        var createFirstElementPatch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("null")),
            PatchOperation.Add(pointer, JsonNode.Parse("\"myValue\"")),
            PatchOperation.Remove(pointer)
        );

        var (_, _, firstResponse) = await CallPatchApi<DataPatchResponse>(createFirstElementPatch, null, HttpStatusCode.OK);

        var firstData = firstResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var firstListItem = firstData.GetProperty("melding").GetProperty("name");
        firstListItem.ValueKind.Should().Be(JsonValueKind.Null);

        var addValuePatch = new JsonPatch(
            PatchOperation.Test(pointer, firstListItem.AsNode()),
            PatchOperation.Replace(pointer, JsonNode.Parse("\"mySecondValue\"")));
        var (_, _, secondResponse) = await CallPatchApi<DataPatchResponse>(addValuePatch, null, HttpStatusCode.OK);
        var secondData = secondResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var secondValue = secondData.GetProperty("melding").GetProperty("name");
        secondValue.GetString().Should().Be("mySecondValue");
    }

    [Fact]
    public async Task SetStringPropertyToEmtpy_ReturnsCorrectDataModel()
    {
        var pointer = JsonPointer.Create("melding", "name");
        var createFirstElementPatch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("null")),
            PatchOperation.Add(pointer, JsonNode.Parse("\"\""))
        );

        var (_, _, firstResponse) = await CallPatchApi<DataPatchResponse>(createFirstElementPatch, null, HttpStatusCode.OK);

        var firstData = firstResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var firstListItem = firstData.GetProperty("melding").GetProperty("name");
        firstListItem.ValueKind.Should().Be(JsonValueKind.Null);;

        var addValuePatch = new JsonPatch(
            PatchOperation.Test(pointer, firstListItem.AsNode()),
            PatchOperation.Replace(pointer, JsonNode.Parse("\"mySecondValue\"")));
        var (_, _, secondResponse) = await CallPatchApi<DataPatchResponse>(addValuePatch, null, HttpStatusCode.OK);
        var secondData = secondResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var secondValue = secondData.GetProperty("melding").GetProperty("name");
        secondValue.GetString().Should().Be("mySecondValue");
    }

    [Fact]
    public async Task SetAttributeTagPropertyToEmtpy_ReturnsCorrectDataModel()
    {
        var pointer = JsonPointer.Create("melding", "tag-with-attribute");
        var createFirstElementPatch = new JsonPatch(
            PatchOperation.Test(pointer, JsonNode.Parse("null")),
            PatchOperation.Add(pointer, JsonNode.Parse("""{"value": "" }"""))
        );

        var (_, _, firstResponse) = await CallPatchApi<DataPatchResponse>(createFirstElementPatch, null, HttpStatusCode.OK);

        var firstData = firstResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var firstListItem = firstData.GetProperty("melding").GetProperty("tag-with-attribute");
        firstListItem.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);

        var addValuePatch = new JsonPatch(
            PatchOperation.Test(pointer, firstListItem.AsNode()),
            PatchOperation.Replace(pointer.Combine("value"), JsonNode.Parse("null")));
        var (_, _, secondResponse) = await CallPatchApi<DataPatchResponse>(addValuePatch, null, HttpStatusCode.OK);
        var secondData = secondResponse.NewDataModel.Should().BeOfType<JsonElement>().Which;
        var secondValue = secondData.GetProperty("melding").GetProperty("name");
        secondValue.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task VerifyLanguageIsPassedToDataProcessor()
    {
        // Update data element with language set to "es"
        var patch = new JsonPatch(
            PatchOperation.Replace(JsonPointer.Create("melding", "name"), JsonNode.Parse("\"Ola Olsen\"")));

        await CallPatchApi<DataPatchResponse>(patch, null, HttpStatusCode.OK, language: "es");

        _dataProcessorMock.Verify(p => p.ProcessDataWrite(It.IsAny<Instance>(), It.Is<Guid>(dataId => dataId == DataGuid), It.IsAny<Skjema>(), It.IsAny<Skjema?>(), "es"), Times.Exactly(1));
        _dataProcessorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidationIssueSeverity_IsSerializedNumeric()
    {
        var patch = new JsonPatch();
        var (_, responseString, _) = await CallPatchApi<DataPatchResponse>(patch, null, HttpStatusCode.OK);

        responseString.Should().Contain("\"severity\":1");
    }
}