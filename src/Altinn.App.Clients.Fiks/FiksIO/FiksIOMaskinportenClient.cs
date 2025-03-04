using Ks.Fiks.Maskinporten.Client;
using IAltinnMaskinportenClient = Altinn.App.Core.Features.Maskinporten.IMaskinportenClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

public class FiksIOMaskinportenClient : IMaskinportenClient
{
    private readonly IAltinnMaskinportenClient _altinnMaskinportenClient;

    public FiksIOMaskinportenClient(IAltinnMaskinportenClient altinnMaskinportenClient)
    {
        _altinnMaskinportenClient = altinnMaskinportenClient;
    }

    public async Task<MaskinportenToken> GetAccessToken(IEnumerable<string> scopes)
    {
        var token = await _altinnMaskinportenClient.GetAccessToken(scopes);
        var expiresIn = token.ExpiresAt - DateTimeOffset.UtcNow;

        return new MaskinportenToken(token.Value, (int)expiresIn.TotalSeconds);
    }

    public Task<MaskinportenToken> GetAccessToken(string scopes)
    {
        return GetAccessToken([scopes]);
    }

    public Task<MaskinportenToken> GetDelegatedAccessToken(string consumerOrg, IEnumerable<string> scopes)
    {
        throw new NotImplementedException();
    }

    public Task<MaskinportenToken> GetDelegatedAccessToken(string consumerOrg, string scopes)
    {
        throw new NotImplementedException();
    }

    public Task<MaskinportenToken> GetOnBehalfOfAccessToken(string consumerOrg, IEnumerable<string> scopes)
    {
        throw new NotImplementedException();
    }

    public Task<MaskinportenToken> GetOnBehalfOfAccessToken(string consumerOrg, string scopes)
    {
        throw new NotImplementedException();
    }
}
