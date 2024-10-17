using System.Net;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Api.Models;
using Altinn.App.Api.Tests.Utils;
using Altinn.Platform.Register.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class OrganisationSearchControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    private const string Org = "tdd";
    private const string App = "contributer-restriction";

    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

    public OrganisationSearchControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    [Fact]
    public async Task Get_OrganisationSearch_WithValidOrgNr_Returns_OrganisationSearchResponse()
    {
        HttpClient client = GetHttpClient();
        var orgNr = "123456789";
        var orgName = "Test Company AS";

        var sendAsyncCalled = false;
        SendAsync = async message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/register/api/v1/organizations/{orgNr}");

            OutputHelper.WriteLine("ER client query string:");
            OutputHelper.WriteLine("Path: " + message.RequestUri.PathAndQuery);

            sendAsyncCalled = true;
            var organisation = new Organization
            {
                Name = orgName,
                OrgNumber = orgNr,
                BusinessAddress = "Test Street 1, 1234 Test City",
                MailingAddress = "Test Street 1, 1234 Test City",
                BusinessPostalCode = "1234",
                BusinessPostalCity = "Test City",
                EMailAddress = "test@company.no",
                FaxNumber = "12345678",
                InternetAddress = "www.company.no",
                MailingPostalCity = "Test City",
                MailingPostalCode = "1234",
                MobileNumber = "12345678",
                TelephoneNumber = "12345678",
                UnitStatus = "Active",
                UnitType = "AS"
            };

            string orgJson = JsonSerializer.Serialize(organisation, _jsonSerializerOptions);
            var responseContent = new StringContent(orgJson);

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };

            return await Task.FromResult(response);
        };

        HttpResponseMessage response = await client.GetAsync($"{Org}/{App}/api/v1/organisations/{orgNr}");

        sendAsyncCalled.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseContent = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseContent);
        var orgSearchResponse = JsonSerializer.Deserialize<OrganisationSearchResponse>(
            responseContent,
            _jsonSerializerOptions
        );

        orgSearchResponse.Should().NotBeNull();
        orgSearchResponse?.Success.Should().BeTrue();
        orgSearchResponse?.OrganisationDetails.Should().NotBeNull();
        orgSearchResponse?.OrganisationDetails?.OrgNr.Should().Be(orgNr);
        orgSearchResponse?.OrganisationDetails?.Name.Should().Be(orgName);
    }

    [Fact]
    public async Task Get_OrganisationSearch_NotFound_Returned_Correctly()
    {
        HttpClient client = GetHttpClient();

        var orgNr = "123456789";

        var sendAsyncCalled = false;
        SendAsync = async message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/register/api/v1/organizations/{orgNr}");
            sendAsyncCalled = true;
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            return await Task.FromResult(response);
        };

        HttpResponseMessage response = await client.GetAsync($"{Org}/{App}/api/v1/organisations/{orgNr}");

        sendAsyncCalled.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseContent = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseContent);
        var orgSearchResponse = JsonSerializer.Deserialize<OrganisationSearchResponse>(
            responseContent,
            _jsonSerializerOptions
        );

        orgSearchResponse.Should().NotBeNull();
        orgSearchResponse?.Success.Should().BeFalse();
        orgSearchResponse?.OrganisationDetails.Should().BeNull();
    }

    private HttpClient GetHttpClient()
    {
        HttpClient client = GetRootedClient(Org, App);
        string token = PrincipalUtil.GetToken(1337, null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
