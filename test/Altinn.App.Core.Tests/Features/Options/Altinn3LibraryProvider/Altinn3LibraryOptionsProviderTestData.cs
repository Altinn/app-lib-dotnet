using System.Net;

namespace Altinn.App.Core.Tests.Features.Options.Altinn3LibraryProvider;

public static class Altinn3LibraryOptionsProviderTestData
{
    public static HttpResponseMessage GetNbEnResponseMessage() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "codes": [
                    {
                      "value": "value1",
                      "label": {
                        "nb": "tekst",
                        "en": "text"
                      },
                      "description": {
                        "nb": "Dette er en tekst",
                        "en": "This is a text"
                      },
                      "helpText": {
                        "en": "Choose this option to get a text",
                        "nb": "Velg dette valget for å få en tekst"
                      },
                      "tags": [
                        "test-data"
                      ]
                    }
                  ],
                  "version": "ttd/code_lists/someNewCodeList/1.json",
                  "source": {
                    "name": "test-data-files"
                  },
                  "tagNames": [
                    "test-data-category"
                  ]
                }
                """
            ),
        };
}
