using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.ExternalApi;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Represents the External API Controller.
/// </summary>
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/api/external")]
[ApiController]
public class ExternalApiController : ControllerBase
{
    private readonly ILogger<ExternalApiController> _logger;
    private readonly IExternalApiService _externalApiService;

    /// <summary>
    /// Creates a new instance of the <see cref="ExternalApiController"/> class
    /// </summary>
    public ExternalApiController(ILogger<ExternalApiController> logger, IExternalApiService externalApiService)
    {
        _logger = logger;
        _externalApiService = externalApiService;
    }

    /// <summary>
    /// Get the data for a specific implementation of an external api, identified by externalApiId
    /// </summary>
    /// <param name="instanceOwnerPartyId">The instance owner party id</param>
    /// <param name="instanceGuid">The instance guid</param>
    /// <param name="externalApiId">The id of the external api</param>
    /// <returns>The data for the external api</returns>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_READ)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Route("{externalApiId}")]
    public async Task<IActionResult> Get(
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string externalApiId
    )
    {
        _logger.LogInformation("Getting data for external api with id {ExternalApiId}", externalApiId);

        var instanceIdentifier = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        try
        {
            var externalApiData = await _externalApiService.GetExternalApiData(externalApiId, instanceIdentifier);
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
