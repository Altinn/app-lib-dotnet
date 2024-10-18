using System.Net.Mime;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// This controller class provides Enhetsregisteret (ER) organisation lookup functionality.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/organisations")]
public class LookupOrganisationController : ControllerBase
{
    private readonly IOrganizationClient _organisationClient;
    private readonly ILogger<LookupOrganisationController> _logger;

    /// <summary>
    /// Initialize a new instance of <see cref="LookupOrganisationController"/> with the given services.
    /// </summary>
    /// <param name="organisationClient">A client for an organisation lookup in ER.</param>
    /// <param name="logger">A logger for logging.</param>
    public LookupOrganisationController(
        IOrganizationClient organisationClient,
        ILogger<LookupOrganisationController> logger
    )
    {
        _organisationClient = organisationClient;
        _logger = logger;
    }

    /// <summary>
    /// Allows an organisation lookup by orgNr in ER
    /// </summary>
    /// <param name="orgNr">Route param that contains the orgNr to look up in ER.</param>
    /// <returns>A <see cref="LookupOrganisationResponse"/> object.</returns>
    [HttpGet]
    [Route("{orgNr}")]
    [ProducesResponseType(typeof(LookupOrganisationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LookupOrganisationResponse>> LookUpOrganisation([FromRoute] string orgNr)
    {
        _logger.LogInformation($"Looking up organisation with orgNr: {orgNr}");
        Organization? organisation = await _organisationClient.GetOrganization(orgNr);

        return Ok(LookupOrganisationResponse.CreateFromOrganisation(organisation));
    }
}
