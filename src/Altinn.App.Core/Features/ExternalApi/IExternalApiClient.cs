namespace Altinn.App.Core.Features.ExternalApi;

/// <summary>
/// Interface for providing external api data
/// </summary>
public interface IExternalApiClient
{
    /// <summary>
    /// The id/name that is used in the <c>externalApiId</c> parameter in the ExternalApiController
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Fetches data from the external api
    /// </summary>
    /// <param name="externalApiId"></param>
    /// <returns></returns>
    Task<object?> GetExternalApiDataAsync(string externalApiId);
}
