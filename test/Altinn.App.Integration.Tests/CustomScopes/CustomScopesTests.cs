using System.IdentityModel.Tokens.Jwt;
using Altinn.App.Api.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests.CustomScopes;

[Trait("Category", "Integration")]
public class CustomScopesTests(ITestOutputHelper _output, AppFixtureClassFixture _classFixture)
    : IClassFixture<AppFixtureClassFixture>
{
    public enum Auth
    {
        User,
        ServiceOwner,
        SystemUser,
        SelfIdentifiedUser,
    }

    [Theory]
    [InlineData(Auth.User, "altinn:portal/enduser")]
    [InlineData(Auth.User, "altinn:instances.read altinn:instances.write")]
    [InlineData(Auth.User, "custom:instances.read custom:instances.write")]
    // TODO: serviceowner
    // [InlineData(Auth.ServiceOwner, "altinn:serviceowner/instances.read altinn:serviceowner/instances.write")]
    public async Task Full(Auth auth, string scope)
    {
        await using var fixtureScope = await _classFixture.Get(_output, TestApps.Basic, scenario: "custom-scopes");
        var fixture = fixtureScope.Fixture;
        var verifier = fixture.ScopedVerifier;
        verifier.UseTestCase(new { auth, scope = scope.Replace('/', ':') });

        var token = auth switch
        {
            Auth.User => await fixture.Auth.GetUserToken(userId: 1337, scope: scope),
            Auth.ServiceOwner => await fixture.Auth.GetServiceOwnerToken(scope: scope),
            Auth.SystemUser => await fixture.Auth.GetSystemUserToken(
                "913312465_sbs",
                "d111dbab-d619-4f15-bf29-58fe570a9ae6",
                scope: scope
            ),
            Auth.SelfIdentifiedUser => await fixture.Auth.GetSelfIdentifiedUserToken("SelvRegistrert", scope: scope),
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
    }
}
