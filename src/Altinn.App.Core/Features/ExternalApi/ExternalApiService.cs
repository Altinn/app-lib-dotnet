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
    /// <returns>An arbitrary json data object</returns>
    Task<object?> GetExternalApiData(string externalApiId, InstanceIdentifier instanceIdentifier);
}

/// <summary>
/// Service for handling external api data
/// </summary>
public class ExternalApiService : IExternalApiService
{
    private readonly ILogger<ExternalApiService> _logger;
    private readonly ExternalApiFactory _externalApiFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalApiService"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="externalApiFactory"></param>
    public ExternalApiService(ILogger<ExternalApiService> logger, ExternalApiFactory externalApiFactory)
    {
        _logger = logger;
        _externalApiFactory = externalApiFactory;
    }

    /// <inheritdoc/>
    public async Task<object?> GetExternalApiData(string externalApiId, InstanceIdentifier instanceIdentifier)
    {
        _logger.LogInformation("Getting data for external api with id {ExternalApiId}", externalApiId);
        var externalApiClient = _externalApiFactory.GetExternalApiClient(externalApiId);

        return await externalApiClient.GetExternalApiDataAsync(instanceIdentifier);
    }
}
