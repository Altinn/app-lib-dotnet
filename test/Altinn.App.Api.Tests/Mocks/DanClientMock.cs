using Altinn.App.Core.Internal.Dan;

namespace Altinn.App.Api.Tests.Mocks;

public class DanClientMock : IDanClient
{
    public Task<Dictionary<string, string>> GetDataset(string dataset, string subject, string fields)
    {
        throw new NotImplementedException();
    }
}
