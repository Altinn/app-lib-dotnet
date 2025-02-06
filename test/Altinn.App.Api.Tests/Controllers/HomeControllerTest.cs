using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using App.IntegrationTests.Mocks.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class HomeControllerTest : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "tdd";
    private const string App = "contributer-restriction";
    private const int InstanceOwnerPartyId = 500600;
    private static readonly Guid _instanceGuid = new("5a2fa5ec-f97c-4816-b57a-dc78a981917e");
    private static readonly string _instanceId = $"{InstanceOwnerPartyId}/{_instanceGuid}";
    private static readonly Guid _dataGuid = new("cd691c32-ae36-4555-8aee-0b7054a413e4");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Define mocks
    private readonly Mock<IDataProcessor> _dataProcessorMock = new(MockBehavior.Strict);
    private readonly Mock<IFormDataValidator> _formDataValidatorMock = new(MockBehavior.Strict);
    private readonly Mock<IAppResources> _appResources = new();

    // Constructor with common setup
    public HomeControllerTest(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper)
    {
        _formDataValidatorMock.Setup(v => v.DataType).Returns("9edd53de-f46f-40a1-bb4d-3efb93dc113d");
        _formDataValidatorMock.Setup(v => v.ValidationSource).Returns("Not a valid validation source");
        OverrideServicesForAllTests = (services) =>
        {
            services.AddSingleton(_dataProcessorMock.Object);
            services.AddSingleton(_formDataValidatorMock.Object);
            services.AddSingleton<IAppResources, AppResourcesMock>();
        };
        TestData.PrepareInstance(Org, App, InstanceOwnerPartyId, _instanceGuid);
    }

    [Fact]
    public async Task SetQueryParams_ensure_bad_request_when_invalid_query_params()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton(
                new AppMetadataMutationHook(appMetadata =>
                {
                    appMetadata.OnEntry = new OnEntry() { Show = "stateless" };
                })
            );
        };

        using var client = GetRootedClient(Org, App);
        var response = await client.GetAsync($"{Org}/{App}/set-query-params?thing=thang");

        var responseString = await response.Content.ReadAsStringAsync();

        OutputHelper.WriteLine(responseString);

        OutputHelper.WriteLine(response.Headers.ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetQueryParams_ensure_bad_request_when_not_stateless()
    {
        using var client = GetRootedClient(Org, App);
        var response = await client.GetAsync($"{Org}/{App}/set-query-params");
        var responseString = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseString);
        OutputHelper.WriteLine(response.Headers.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetQueryParams_ensure_bad_request_when_no_query_params()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton(
                new AppMetadataMutationHook(appMetadata =>
                {
                    appMetadata.OnEntry = new OnEntry() { Show = "stateless" };
                })
            );
        };

        using var client = GetRootedClient(Org, App);
        var response = await client.GetAsync($"{Org}/{App}/set-query-params");
        var responseString = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseString);
        OutputHelper.WriteLine(response.Headers.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetQueryParams_ensure_ok_request_when_query_params_are_valid()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton(
                new AppMetadataMutationHook(appMetadata =>
                {
                    appMetadata.OnEntry = new OnEntry() { Show = "stateless" };
                    var mockDataType = new DataType();
                    mockDataType.Id = "my-data-type";
                    appMetadata.DataTypes = new List<DataType> { mockDataType };
                })
            );

            services.AddSingleton(
                new AppResourcesMutationHook(mock =>
                {
                    mock.AddOrUpdatePrefill("my-data-type", "{\"QueryParameters\":{\"thing\":\"$.someField\"}}");
                })
            );
        };

        using var client = GetRootedClient(Org, App);
        var response = await client.GetAsync($"{Org}/{App}/set-query-params?thing=whatever");
        var responseString = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseString);
        OutputHelper.WriteLine(response.Headers.ToString());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetQueryParams_ensure_only_one_content_security_policy()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton(
                new AppMetadataMutationHook(appMetadata =>
                {
                    appMetadata.OnEntry = new OnEntry() { Show = "stateless" };
                    var mockDataType = new DataType();
                    mockDataType.Id = "my-data-type";
                    appMetadata.DataTypes = new List<DataType> { mockDataType };
                })
            );

            services.AddSingleton(
                new AppResourcesMutationHook(mock =>
                {
                    mock.AddOrUpdatePrefill("my-data-type", "{\"QueryParameters\":{\"thing\":\"$.someField\"}}");
                })
            );
        };

        using var client = GetRootedClient(Org, App);
        var response = await client.GetAsync($"{Org}/{App}/set-query-params?thing=whatever");
        var responseString = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseString);
        OutputHelper.WriteLine(response.Headers.ToString());

        //response.Headers.GetValues().

        var cspHeaderCount = response.Headers.Count(h => h.Key == "Content-Security-Policy");
        Assert.Equal(1, cspHeaderCount);

        //Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
