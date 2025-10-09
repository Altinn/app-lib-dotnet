using System.Net;
using System.Text.Json;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Tests.Features.Correspondence;

public static class TestHelpers
{
    public static OrganizationNumber GetOrganizationNumber(int index) =>
        IdentificationNumberProvider.OrganizationNumbers.GetValidNumber(index);

    public static NationalIdentityNumber GetNationalIdentityNumber(int index) =>
        IdentificationNumberProvider.NationalIdentityNumbers.GetValidNumber(index);

    public static HttpContent? GetItem(this MultipartFormDataContent content, string name)
    {
        return content.FirstOrDefault(item => item.Headers.ContentDisposition?.Name?.Trim('\"') == name);
    }

    public static HttpResponseMessage ResponseMessageFactory<T>(
        T content,
        HttpStatusCode statusCode = HttpStatusCode.OK
    )
    {
        string test = content as string ?? JsonSerializer.Serialize(content);

        return new HttpResponseMessage(statusCode) { Content = new StringContent(test) };
    }

    public static Task<JwtToken> OrgTokenFactory(IEnumerable<string> scopes)
    {
        var formattedScopes = MaskinportenClient.GetFormattedScopes(scopes);
        string token;

        try
        {
            token = TestAuthentication.GetServiceOwnerToken(scope: formattedScopes);
        }
        catch (InvalidOperationException)
        {
            token = TestAuthentication.GetOrgToken(scope: formattedScopes);
        }

        return Task.FromResult(JwtToken.Parse(token));
    }

    public static SendCorrespondenceResponse DummySendCorrespondenceResponse =>
        new()
        {
            Correspondences = new List<CorrespondenceDetailsResponse>
            {
                new()
                {
                    CorrespondenceId = Guid.Empty,
                    Status = CorrespondenceStatus.Initialized,
                    Recipient = OrganizationOrPersonIdentifier.Create(GetOrganizationNumber(0)),
                },
            },
        };

    public static GetCorrespondenceStatusResponse DummyGetCorrespondenceStatusResponse =>
        new()
        {
            CorrespondenceId = Guid.Empty,
            Created = DateTimeOffset.MinValue,
            StatusChanged = DateTimeOffset.MinValue,
            Recipient = GetOrganizationNumber(0),
            Sender = GetOrganizationNumber(1),
            Status = CorrespondenceStatus.Published,
            StatusHistory = [],
            ResourceId = string.Empty,
            SendersReference = string.Empty,
        };
}
