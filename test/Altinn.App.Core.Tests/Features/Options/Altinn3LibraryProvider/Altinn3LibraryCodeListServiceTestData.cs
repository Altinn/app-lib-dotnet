using System.Net;
using System.Text.Json;
using Altinn.App.Core.Features.Options.Altinn3LibraryProvider;

namespace Altinn.App.Core.Tests.Features.Options.Altinn3LibraryProvider;

public static class Altinn3LibraryCodeListServiceTestData
{
    public static Func<HttpResponseMessage> GetNbEnResponseMessage()
    {
        return () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(GetNbEnAltinn3LibraryCodeListResponse()),
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),
            };
    }

    public static Altinn3LibraryCodeListResponse GetNbEnAltinn3LibraryCodeListResponse()
    {
        var labels = new Dictionary<string, string> { { "nb", "tekst" }, { "en", "text" } };
        var descriptions = new Dictionary<string, string> { { "nb", "Dette er en tekst" }, { "en", "This is a text" } };
        var helpTexts = new Dictionary<string, string>
        {
            { "en", "Choose this option to get a text" },
            { "nb", "Velg dette valget for å få en tekst" },
        };

        return GetAltinn3LibraryCodeListResponse(labels, descriptions, helpTexts);
    }

    public static Altinn3LibraryCodeListResponse GetAltinn3LibraryCodeListResponse(
        Dictionary<string, string>? labels,
        Dictionary<string, string>? descriptions,
        Dictionary<string, string>? helpTexts,
        List<string>? tagNames = null,
        List<string>? tags = null
    )
    {
        return new Altinn3LibraryCodeListResponse
        {
            Codes = new List<Altinn3LibraryCodeListItem>()
            {
                new()
                {
                    Value = "value1",
                    Label = labels,
                    Description = descriptions,
                    HelpText = helpTexts,
                    Tags = tags,
                },
            },
            Version = "ttd/code_lists/someNewCodeList/1.json",
            Source = new Altinn3LibraryCodeListSource { Name = "test-data-files" },
            TagNames = tagNames,
        };
    }
}
