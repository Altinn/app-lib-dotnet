using FiksMaskinportenToken = Ks.Fiks.Maskinporten.Client.MaskinportenToken;
using IAltinnMaskinportenClient = Altinn.App.Core.Features.Maskinporten.IMaskinportenClient;
using IFiksMaskinportenClient = Ks.Fiks.Maskinporten.Client.IMaskinportenClient;

namespace Altinn.App.Clients.Fiks.FiksIO;

internal sealed class FiksIOMaskinportenClient : IFiksMaskinportenClient
{
    private readonly IAltinnMaskinportenClient _altinnMaskinportenClient;

    public FiksIOMaskinportenClient(IAltinnMaskinportenClient altinnMaskinportenClient)
    {
        _altinnMaskinportenClient = altinnMaskinportenClient;
    }

    public async Task<FiksMaskinportenToken> GetAccessToken(IEnumerable<string> scopes)
    {
        var token = await _altinnMaskinportenClient.GetAccessToken(scopes);
        var expiresIn = token.ExpiresAt - DateTimeOffset.UtcNow;

        return new FiksMaskinportenToken(token.Value, (int)expiresIn.TotalSeconds);
    }

    public Task<FiksMaskinportenToken> GetAccessToken(string scopes)
    {
        return GetAccessToken([scopes]);
    }

    public Task<FiksMaskinportenToken> GetDelegatedAccessToken(string consumerOrg, IEnumerable<string> scopes)
    {
        throw new NotImplementedException();
    }

    public Task<FiksMaskinportenToken> GetDelegatedAccessToken(string consumerOrg, string scopes)
    {
        throw new NotImplementedException();
    }

    public Task<FiksMaskinportenToken> GetOnBehalfOfAccessToken(string consumerOrg, IEnumerable<string> scopes)
    {
        throw new NotImplementedException();
    }

    public Task<FiksMaskinportenToken> GetOnBehalfOfAccessToken(string consumerOrg, string scopes)
    {
        throw new NotImplementedException();
    }
}
