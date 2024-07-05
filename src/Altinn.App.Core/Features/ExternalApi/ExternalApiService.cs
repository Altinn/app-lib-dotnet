using Altinn.App.Core.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.ExternalApi;

/// <summary>
/// Interface for handling external api data
/// </summary>
public interface IExternalApiService
{
    /// <summary>
    /// Get data for an external api
    /// </summary>
    /// <param name="externalApiId"></param>
    /// <param name="instanceIdentifier"></param>
    /// <param name="queryParams"></param>
    /// <returns>An arbitrary json data object</returns>
    Task<object?> GetExternalApiData(
        string externalApiId,
        InstanceIdentifier instanceIdentifier,
        Dictionary<string, string> queryParams
    );
}

/// <summary>
/// Service for handling external api data
/// </summary>
public class ExternalApiService(ILogger<ExternalApiService> logger, IExternalApiFactory externalApiFactory)
    : IExternalApiService
{
    private readonly ILogger<ExternalApiService> _logger = logger;
    private readonly IExternalApiFactory _externalApiFactory = externalApiFactory;

    /// <inheritdoc/>
    public async Task<object?> GetExternalApiData(
        string externalApiId,
        InstanceIdentifier instanceIdentifier,
        Dictionary<string, string> queryParams
    )
    {
        var externalApiClient = _externalApiFactory.GetExternalApiClient(externalApiId);
        if (externalApiClient is null)
        {
            _logger.LogWarning("External api with id {ExternalApiId} not found", externalApiId);
            throw new KeyNotFoundException($"External api with id {externalApiId} not found");
        }

        _logger.LogInformation("Getting data from external api with id {ExternalApiId}", externalApiId);
        return await externalApiClient.GetExternalApiDataAsync(instanceIdentifier, queryParams);
    }
}
