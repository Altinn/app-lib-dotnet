using System.Reflection;
using System.Text.Json;
using Altinn.App.Core.Features.Options.AltinnCdnProvider;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Tests.Features.Options.AltinnCdnProvider;

public class AltinnCdnApiClientMock : IAltinnCdnApiClient
{
    private const string YesNoMaybeCodelist =
        "Altinn.App.Core.Tests.Features.Options.AltinnCdnProvider.Testdata.yesNoMaybe.en.json";

    private const string NumbersCodelist =
        "Altinn.App.Core.Tests.Features.Options.AltinnCdnProvider.Testdata.numbers.en.json";

    public async Task<List<AppOption>> GetCodeList(string org, string codeListId, string version, string language)
    {
        if (org == "ttd" && version == "1970-01-01T00-00-00Z" && language == "en")
        {
            return codeListId switch
            {
                "yesNoMaybe" => await LoadOptionsFromEmbeddedJson(YesNoMaybeCodelist),
                "numbers" => await LoadOptionsFromEmbeddedJson(NumbersCodelist),
                _ => [],
            };
        }

        return [];
    }

    private static async Task<List<AppOption>> LoadOptionsFromEmbeddedJson(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
        }

        return await JsonSerializer.DeserializeAsync<List<AppOption>>(stream) ?? [];
    }
}
