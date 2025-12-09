namespace Altinn.App.Core.Internal.Dan;

public interface IDanClient
{
    public Task<Dictionary<string, string>> GetDataset(string dataset, string subject);
}
