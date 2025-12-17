namespace Altinn.App.Core.Internal.Dan;

/// <summary>
/// DanClient interface
/// </summary>
public interface IDanClient
{
    /// <summary>
    /// Method for getting a selected dataset from Dan Api
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="subject"></param>
    /// <returns></returns>
    public Task<Dictionary<string, string>> GetDataset(string dataset, string subject);
}
