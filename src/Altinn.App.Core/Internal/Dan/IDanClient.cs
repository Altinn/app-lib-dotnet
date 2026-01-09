namespace Altinn.App.Core.Internal.Dan;

/// <summary>
/// DanClient interface
/// </summary>
public interface IDanClient
{
    /// <summary>
    /// Method for getting a selected dataset from Dan Api
    /// </summary>
    /// <param name="dataset">Name of the dataset</param>
    /// <param name="subject">Usually ssn or OrgNumber</param>
    /// <param name="jmesPathExpression">jmesPathExpression</param>
    /// <returns></returns>
    public Task<Dictionary<string, string>> GetDataset(string dataset, string subject, string fields);
}
