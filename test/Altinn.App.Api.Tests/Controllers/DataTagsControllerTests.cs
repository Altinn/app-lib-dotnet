using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.App.Api.Models;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class DataTagsControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
    : ApiTestBase(factory, outputHelper),
        IClassFixture<WebApplicationFactory<Program>>
{
    readonly string org = "tdd";
    readonly string app = "contributer-restriction";
    readonly int instanceOwnerPartyId = 500600;
    readonly Guid instanceGuid = new("fad57e80-ec2f-4dee-90ac-400fa6d7720f");
    readonly Guid dataGuid = new("3b46b9ef-774c-4849-b4dd-66ef871f5b07");

    [Fact]
    public async Task SetTags_ValidRequest_ReturnsOkWithTags()
    {
        // Arrange
        HttpClient client = GetRootedClient(org, app);
        string token = TestAuthentication.GetServiceOwnerToken("405003309", org: "nav");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, token);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        var setTagsRequest = new SetTagsRequest { Tags = ["tagA", "tagB", "tagC"] };

        var json = JsonSerializer.Serialize(setTagsRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/tags",
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var setTagsResponse = JsonSerializer.Deserialize<SetTagsResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        setTagsResponse.Should().NotBeNull();
        setTagsResponse.Tags.Should().BeEquivalentTo(["tagA", "tagB", "tagC"]);
        setTagsResponse.ValidationIssues.Should().NotBeNull();
    }

    [Fact]
    public async Task SetTags_EmptyTagsList_ClearsAllTags()
    {
        // Arrange
        HttpClient client = GetRootedClient(org, app);
        string token = TestAuthentication.GetServiceOwnerToken("405003309", org: "nav");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, token);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        var setTagsRequest = new SetTagsRequest { Tags = [] };

        var json = JsonSerializer.Serialize(setTagsRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/tags",
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var setTagsResponse = JsonSerializer.Deserialize<SetTagsResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        setTagsResponse.Should().NotBeNull();
        setTagsResponse.Tags.Should().BeEmpty();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("tag@")]
    [InlineData("tag with spaces")]
    [InlineData("tag!")]
    public async Task SetTags_InvalidTagFormat_ReturnsBadRequest(string invalidTag)
    {
        // Arrange
        HttpClient client = GetRootedClient(org, app);
        string token = TestAuthentication.GetServiceOwnerToken("405003309", org: "nav");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, token);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        var setTagsRequest = new SetTagsRequest { Tags = ["validTag", invalidTag] };

        var json = JsonSerializer.Serialize(setTagsRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/tags",
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("letters");
    }

    [Fact]
    public async Task SetTags_DataElementNotFound_ReturnsNotFound()
    {
        // Arrange
        Guid nonExistentDataGuid = new("99999999-9999-9999-9999-999999999999"); // Non-existent data element

        HttpClient client = GetRootedClient(org, app);
        string token = TestAuthentication.GetServiceOwnerToken("405003309", org: "nav");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, token);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        var setTagsRequest = new SetTagsRequest { Tags = ["tagA"] };

        var json = JsonSerializer.Serialize(setTagsRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{nonExistentDataGuid}/tags",
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Unable to find data element");
    }

    [Theory]
    [InlineData("valid-tag")]
    [InlineData("validTag")]
    [InlineData("valid_tag")]
    [InlineData("åæø")]
    [InlineData("TagWithÜmlaut")]
    public async Task SetTags_ValidTagFormats_ReturnsOk(string validTag)
    {
        // Arrange
        HttpClient client = GetRootedClient(org, app);
        string token = TestAuthentication.GetServiceOwnerToken("405003309", org: "nav");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, token);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        var setTagsRequest = new SetTagsRequest { Tags = [validTag] };

        var json = JsonSerializer.Serialize(setTagsRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/tags",
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var setTagsResponse = JsonSerializer.Deserialize<SetTagsResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        setTagsResponse.Should().NotBeNull();
        setTagsResponse.Tags.Should().Contain(validTag);
    }
}
