using Altinn.App.Core.Models;
using Altinn.App.Core.Tests.Models;

namespace Altinn.App.Core.Tests.Features.Correspondence;

public static class TestHelpers
{
    public static OrganisationNumber GetOrganisationNumber(int index)
    {
        var i = index % OrganisationNumberTests.ValidOrganisationNumbers.Length;
        return OrganisationNumber.Parse(OrganisationNumberTests.ValidOrganisationNumbers[i]);
    }

    public static HttpContent? GetItem(this MultipartFormDataContent content, string name)
    {
        return content.FirstOrDefault(item => item.Headers.ContentDisposition?.Name?.Trim('\"') == name);
    }
}
