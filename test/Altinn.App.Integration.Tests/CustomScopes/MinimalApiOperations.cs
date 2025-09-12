using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Integration.Tests.AppFixture;

namespace Altinn.App.Integration.Tests.CustomScopes;

internal static class MinimalApiOperations
{
    public static async Task Call(
        AppFixture fixture,
        ScopedVerifier verifier,
        string token,
        ReadApiResponse<Instance>? instantiationData,
        [CallerFilePath] string sourceFile = ""
    )
    {
        // Test minimal API endpoints
        var instance = instantiationData?.Data.Model;
        var instanceGuid = instance?.Id is not null
            ? Guid.Parse(instance.Id.Split('/')[1])
            : Guid.Parse("12345678-1234-1234-1234-123456789012");
        var instanceOwnerPartyId = instance?.InstanceOwner?.PartyId ?? "501337";
        var scrubbers =
            instantiationData is not null && instance is not null
                ? new Scrubbers(StringScrubber: Scrubbers.InstanceStringScrubber(instantiationData))
                : null;

        // GET endpoint with instanceGuid - should be protected with read scope
        using var getDataResponse = await fixture.Generic.Get(
            $"/ttd/basic/api/instances/{instanceGuid}/minimal-data",
            token
        );
        await verifier.Verify(
            getDataResponse,
            snapshotName: "GetMinimalData",
            scrubbers: scrubbers,
            sourceFile: sourceFile
        );

        // POST endpoint with instanceGuid - should be protected with write scope
        using var postProcessResponse = await fixture.Generic.Post(
            $"/ttd/basic/api/instances/{instanceGuid}/minimal-process",
            token,
            new StringContent("[]", MediaTypeHeaderValue.Parse("application/json"))
        );
        await verifier.Verify(
            postProcessResponse,
            snapshotName: "PostMinimalProcess",
            scrubbers: scrubbers,
            sourceFile: sourceFile
        );

        // GET endpoint with instanceOwnerPartyId - should be protected with read scope
        using var getSummaryResponse = await fixture.Generic.Get(
            $"/ttd/basic/api/parties/{instanceOwnerPartyId}/minimal-summary",
            token
        );
        await verifier.Verify(
            getSummaryResponse,
            snapshotName: "GetMinimalSummary",
            scrubbers: scrubbers,
            sourceFile: sourceFile
        );

        // Anonymous endpoint - should NOT be protected
        using var getPublicResponse = await fixture.Generic.Get("/ttd/basic/api/minimal-public", token);
        await verifier.Verify(getPublicResponse, snapshotName: "GetMinimalPublic", scrubbers: scrubbers);
    }
}
