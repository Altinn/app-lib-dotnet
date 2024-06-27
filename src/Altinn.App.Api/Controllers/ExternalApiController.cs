using Altinn.App.Core.Features.ExternalApi;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Represents the DataLists API.
/// </summary>
[Route("{org}/{app}/api/external")]
[ApiController]
public class ExternalApiController : ControllerBase
{
    private readonly ILogger<ExternalApiController> _logger;
    private readonly IExternalApiService _externalApiService;

    /// <summary>
    /// Create new instance of the <see cref="ActionsController"/> class
    /// </summary>
    public ExternalApiController(ILogger<ExternalApiController> logger, IExternalApiService externalApiService)
    {
        _logger = logger;
        _externalApiService = externalApiService;
    }

    /// <summary>
    /// Get the data for a specific external api
    /// </summary>
    /// <param name="externalApiId">The id of the external api</param>
    /// <returns>The data for the external api</returns>
    // [Authorize(Policy = "InstanceRead")] // TODO: Implement authorization
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Route("{externalApiId}")]
    public async Task<IActionResult> Get([FromRoute] string externalApiId)
    {
        _logger.LogInformation("Getting data for external api with id {ExternalApiId}", externalApiId);

        try
        {
            var externalApiData = await _externalApiService.GetExternalApiData(externalApiId);
            return Ok(externalApiData);
        }
        catch (Exception e)
        {
            if (e is KeyNotFoundException)
            {
                return NotFound();
            }

            return StatusCode(500, "An error occurred while fetching data from an external api.");
        }
    }
}
