using Altinn.App.Api.Models;
using Altinn.Platform.Storage.Interface.Models;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests.Singning;

[Trait("Category", "Integration")]
public class SimpleTests(ITestOutputHelper _output, AppFixtureClassFixture _classFixture)
    : IClassFixture<AppFixtureClassFixture>
{
    public enum Auth
    {
        OldUser,
        OldServiceOwner,
        User,
        ServiceOwner,
        SystemUser,
        SelfIdentifiedUser,
    }

    [Theory]
    [CombinatorialData]
    // TODO: add back systemuser to auth parameter when it is supported in signing
    // There are snapshots for it in _snapshots folder where you can see the issue, and there is a comment with some explanation
    // on the PR that introduced this code
    public async Task Full([CombinatorialValues(Auth.User)] Auth auth)
    {
        await using var fixtureScope = await _classFixture.Get(_output, TestApps.Basic, "signing-simple");
        var fixture = fixtureScope.Fixture;
        var verifier = fixture.ScopedVerifier;
        verifier.UseTestCase(new { auth });

        var token = auth switch
        {
            Auth.User => await fixture.Auth.GetUserToken(1337, authenticationLevel: 3, scope: "altinn:portal/enduser"),
            Auth.SystemUser => await fixture.Auth.GetSystemUserToken(
                systemId: "913312465_sbs",
                systemUserId: "d111dbab-d619-4f15-bf29-58fe570a9ae6",
                scope: "altinn:instances.read altinn:instances.write"
            ),
            _ => throw new NotSupportedException($"Auth {auth} is not supported"),
        };

        var instanceOwner = auth switch
        {
            Auth.User => new InstanceOwner { PartyId = "501337" },
            Auth.SystemUser => new InstanceOwner { OrganisationNumber = "950474084" },
            _ => throw new NotSupportedException($"Auth {auth} is not supported"),
        };

        using var instantiationResponse = await fixture.Instances.PostSimplified(
            token,
            new InstansiationInstance
            {
                InstanceOwner = instanceOwner,
                Prefill = new() { ["property1"] = "1" },
            }
        );

        using var instantiationData = await instantiationResponse.Read<Instance>();
        var instance = instantiationData.Data.Model;
        Assert.NotNull(instance);
        var scrubbers = new Scrubbers(StringScrubber: Scrubbers.InstanceStringScrubber(instance));
        await verifier.Verify(instantiationData, snapshotName: "Instance", scrubbers: scrubbers);

        using var processNextResponse = await fixture.Instances.ProcessNext(
            token,
            instantiationData,
            new ProcessNext { Action = "sign" }
        );
        using var processNextData = await processNextResponse.Read<AppProcessState>();

        await verifier.Verify(processNextData, snapshotName: "ProcessNext", scrubbers: scrubbers);

        using var download = await fixture.Instances.Download(token, instantiationData);
        await download.Verify(verifier);

        await verifier.Verify(fixture.GetSnapshotAppLogs(), snapshotName: "Logs");
    }
}
