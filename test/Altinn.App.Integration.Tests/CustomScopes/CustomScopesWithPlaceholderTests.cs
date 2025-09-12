using System.IdentityModel.Tokens.Jwt;
using Altinn.App.Api.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests.CustomScopes;

[Trait("Category", "Integration")]
public class CustomScopesWithPlaceholderTests(ITestOutputHelper _output, AppFixtureClassFixture _classFixture)
    : IClassFixture<AppFixtureClassFixture>
{
    public enum Auth
    {
        User,
        ServiceOwner,
        SystemUser,
        SelfIdentifiedUser,
    }

    // csharpier-ignore
    public static TheoryData<Auth, string> TestData = new TheoryData<Auth, string>
        {
            { Auth.User, "custom:basic/instances.read custom:basic/instances.write" },
            { Auth.SystemUser, "custom:basic/instances.read custom:basic/instances.write" },
            { Auth.ServiceOwner, "custom:basic/serviceowner/instances.read custom:basic/serviceowner/instances.write" },
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task Full(Auth auth, string scope)
    {
        await using var fixtureScope = await _classFixture.Get(
            _output,
            TestApps.Basic,
            scenario: "custom-scopes-placeholders"
        );
        var fixture = fixtureScope.Fixture;
        var verifier = fixture.ScopedVerifier;
        // sanitizedScope must be valid part of path on e.g. Windows. Variable is used in spanshot filename
        var sanitizedScope = scope.Replace(':', '-').Replace(' ', '-').Replace('/', '-');
        verifier.UseTestCase(new { auth, scope = sanitizedScope });

        var token = auth switch
        {
            Auth.User => await fixture.Auth.GetUserToken(userId: 1337, scope: scope),
            Auth.SystemUser => await fixture.Auth.GetSystemUserToken(
                "913312465_sbs",
                "d111dbab-d619-4f15-bf29-58fe570a9ae6",
                scope: scope
            ),
            Auth.ServiceOwner => await fixture.Auth.GetServiceOwnerToken(scope: scope),
            _ => throw new ArgumentOutOfRangeException(nameof(auth)),
        };
        var handler = new JwtSecurityTokenHandler();
        var tokenObj = handler.ReadJwtToken(token);
        await verifier.Verify(tokenObj.Payload, snapshotName: "Token").ScrubMembers("exp", "nbf", "iat");

        var instantiationResponse = await fixture.Instances.PostSimplified(
            token,
            new InstansiationInstance
            {
                InstanceOwner = new InstanceOwner { PartyId = "501337" },
                Prefill = new() { { "property1", "1" }, { "property2", "1" } },
            }
        );
        using var readInstantiationResponse = await instantiationResponse.Read<Instance>();
        // Verifies the instantiation response
        await verifier.Verify(
            readInstantiationResponse,
            snapshotName: "Instantiation",
            scrubbers: new Scrubbers(StringScrubber: Scrubbers.InstanceStringScrubber(readInstantiationResponse))
        );

        // Test minimal API endpoints
        var instance = readInstantiationResponse.Data.Model;
        var instanceGuid = instance?.Id is not null
            ? Guid.Parse(instance.Id.Split('/')[1])
            : Guid.Parse("12345678-1234-1234-1234-123456789012");
        var instanceOwnerPartyId = instance?.InstanceOwner?.PartyId ?? "501337";

        async Task<AppFixture.ApiResponse> CallGetAsync(string endpoint)
        {
            var client = fixture.GetAppClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request);
            return new AppFixture.ApiResponse(fixture, response);
        }

        async Task<AppFixture.ApiResponse> CallPostAsync(string endpoint, object data)
        {
            var client = fixture.GetAppClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var payload = System.Text.Json.JsonSerializer.Serialize(data);
            request.Content = new StringContent(
                payload,
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
            );
            var response = await client.SendAsync(request);
            return new AppFixture.ApiResponse(fixture, response);
        }

        // GET endpoint with instanceGuid - should be protected with read scope
        using var getDataResponse = await CallGetAsync($"/ttd/basic/api/instances/{instanceGuid}/minimal-data");
        await verifier.Verify(getDataResponse, snapshotName: "GetMinimalData", scrubbers: null);

        // POST endpoint with instanceGuid - should be protected with write scope
        using var postProcessResponse = await CallPostAsync(
            $"/ttd/basic/api/instances/{instanceGuid}/minimal-process",
            new { data = "test" }
        );
        await verifier.Verify(postProcessResponse, snapshotName: "PostMinimalProcess", scrubbers: null);

        // GET endpoint with instanceOwnerPartyId - should be protected with read scope
        using var getSummaryResponse = await CallGetAsync(
            $"/ttd/basic/api/parties/{instanceOwnerPartyId}/minimal-summary"
        );
        await verifier.Verify(getSummaryResponse, snapshotName: "GetMinimalSummary", scrubbers: null);

        // Anonymous endpoint - should NOT be protected
        using var getPublicResponse = await CallGetAsync("/ttd/basic/api/minimal-public");
        await verifier.Verify(getPublicResponse, snapshotName: "GetMinimalPublic", scrubbers: null);

        await verifier.VerifyLogs();
    }
}
