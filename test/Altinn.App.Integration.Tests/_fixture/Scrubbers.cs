using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Integration.Tests.AppFixture;

namespace Altinn.App.Integration.Tests;

internal static class Scrubbers
{
    // A scrubber function that replaces information part of an instance that is not stable across test runs
    public static Func<string, string> InstanceScrubber(Instance instance) =>
        v =>
        {
            v = v.Replace(instance.Id.Split('/')[1], "<instanceGuid>");
            for (int i = 0; i < instance.Data.Count; i++)
                v = v.Replace(instance.Data[i].Id, $"<dataElementId[{i}]>");
            return v;
        };

    internal static Func<string, string> InstanceScrubber(ReadApiResponse<Instance> readResponse) =>
        readResponse.Data.Model is not null ? InstanceScrubber(readResponse.Data.Model) : v => v;
}
