using System.Net.Mime;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// This controller class provides Enhetsregisteret (ER) organisation search functionality.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/organisations")]
public class OrganisationSearchController : ControllerBase
{
    private readonly IOrganizationClient _organisationClient;
    private readonly ILogger<OrganisationSearchController> _logger;

    /// <summary>
    /// Initialize a new instance of <see cref="OrganisationSearchController"/> with the given services.
    /// </summary>
    /// <param name="organisationClient">A client for searching for an organisation in ER.</param>
    /// <param name="logger">A logger for logging.</param>
    public OrganisationSearchController(
        IOrganizationClient organisationClient,
        ILogger<OrganisationSearchController> logger
    )
    {
        _organisationClient = organisationClient;
        _logger = logger;
    }

    /// <summary>
    /// Allows searching for an organisation in ER
    /// </summary>
    /// <param name="orgNr">Payload that contains params for executing a search for an organisation.</param>
    /// <returns>A <see cref="OrganisationSearchResponse"/> object.</returns>
    [HttpGet]
    [Route("{orgNr}")]
    [ProducesResponseType(typeof(OrganisationSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganisationSearchResponse>> SearchForOrganisation([FromRoute] string orgNr)
    {
        _logger.LogInformation($"Searching for organisation with orgNr: {orgNr}");
        Organization? organisation = await _organisationClient.GetOrganization(orgNr);

        return Ok(OrganisationSearchResponse.CreateFromOrganisation(organisation));
    }
}
