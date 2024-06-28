using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.ExternalApi;

/// <summary>
/// Factory class for resolving <see cref="IExternalApiClient"/> implementations
/// </summary>
public class ExternalApiFactory
{
    private readonly ILogger<ExternalApiFactory> _logger;
    private IEnumerable<IExternalApiClient> _externalApiClients { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalApiFactory"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="externalApiClients"></param>
    public ExternalApiFactory(ILogger<ExternalApiFactory> logger, IEnumerable<IExternalApiClient> externalApiClients)
    {
        _logger = logger;
        _externalApiClients = externalApiClients;
    }

    /// <summary>
    ///  Finds the implementation of IExternalApiClient based on the external api id
    /// </summary>
    /// <param name="externalApiId"></param>
    public IExternalApiClient GetExternalApiClient(string externalApiId)
    {
        IExternalApiClient? client = _externalApiClients.FirstOrDefault(e =>
            e.Id.Equals(externalApiId, StringComparison.OrdinalIgnoreCase)
        );

        if (client == null)
        {
            _logger.LogWarning("No external client found for external API with id {ExternalApiId}", externalApiId);
            throw new KeyNotFoundException($"No external api client found for external API with id {externalApiId}");
        }

        return client;
    }
}
