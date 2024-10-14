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

public class PersonSearchControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
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

    public PersonSearchControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    [Fact]
    public async Task Post_PersonSearch_HappyPath_ReturnsOk()
    {
        HttpClient client = GetHttpClient();

        var sendAsyncCalled = false;
        SendAsync = async message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/register/api/v1/persons");
            string socialSecurityNumber = message.Headers.GetValues("X-Ai-NationalIdentityNumber").First();
            string lastName = message.Headers.GetValues("X-Ai-LastName").First();

            OutputHelper.WriteLine("Person client request headers:");
            OutputHelper.WriteLine("X-Ai-NationalIdentityNumber: " + socialSecurityNumber);
            OutputHelper.WriteLine("X-Ai-LastName: " + lastName + " (base64)");

            sendAsyncCalled = true;

            var person = new Person
            {
                SSN = "12345678901",
                Name = "Ola Normann",
                FirstName = "Ola Normann",
                MiddleName = null,
                LastName = "Normann"
            };

            string personJson = JsonSerializer.Serialize(person);
            var responseContent = new StringContent(personJson);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = responseContent;

            return await Task.FromResult(response);
        };

        using var requestContent = new StringContent(
            """{"SocialSecurityNumber": "12345678901", "LastName": "Normann"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage response = await client.PostAsync($"{Org}/{App}/api/v1/person-search", requestContent);

        sendAsyncCalled.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseContent = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseContent);
        var personSearchResponse = JsonSerializer.Deserialize<PersonSearchResponse>(
            responseContent,
            _jsonSerializerOptions
        );

        personSearchResponse.Should().NotBeNull();
        personSearchResponse?.Success.Should().BeTrue();
        personSearchResponse?.PersonDetails.Should().NotBeNull();
        personSearchResponse?.PersonDetails?.Ssn.Should().Be("12345678901");
        personSearchResponse?.PersonDetails?.LastName.Should().Be("Normann");
    }

    [Fact]
    public async Task Post_PersonSearch_NotFound_Returned_Correctly()
    {
        HttpClient client = GetHttpClient();

        var sendAsyncCalled = false;
        SendAsync = async message =>
        {
            message.RequestUri!.PathAndQuery.Should().Be($"/register/api/v1/persons");
            sendAsyncCalled = true;

            var response = new HttpResponseMessage(HttpStatusCode.NotFound);

            return await Task.FromResult(response);
        };

        using var requestContent = new StringContent(
            """{"SocialSecurityNumber": "12345678901", "LastName": "Normann"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage response = await client.PostAsync($"{Org}/{App}/api/v1/person-search", requestContent);

        sendAsyncCalled.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseContent = await response.Content.ReadAsStringAsync();
        OutputHelper.WriteLine(responseContent);
        var personSearchResponse = JsonSerializer.Deserialize<PersonSearchResponse>(
            responseContent,
            _jsonSerializerOptions
        );

        personSearchResponse.Should().NotBeNull();
        personSearchResponse?.Success.Should().BeFalse();
        personSearchResponse?.PersonDetails.Should().BeNull();
    }

    private HttpClient GetHttpClient()
    {
        HttpClient client = GetRootedClient(Org, App);
        string token = PrincipalUtil.GetToken(1337, null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
