using System.Net.Mime;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// This controller class provides Folkeregisteret (DSF) person lookup functionality.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/lookup/person")]
public class LookupPersonController : ControllerBase
{
    private readonly IPersonClient _personClient;

    /// <summary>
    /// Initialize a new instance of <see cref="LookupPersonController"/> with the given services.
    /// </summary>
    /// <param name="personClient">A client for looking up a person.</param>
    public LookupPersonController(IPersonClient personClient)
    {
        _personClient = personClient;
    }

    /// <summary>
    /// Lookup a person in Folkeregisteret (DSF)
    /// </summary>
    /// <param name="lookupPersonRequest">Payload that contains params for executing a person lookup.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="LookupPersonResponse"/> object.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(LookupPersonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LookupPersonResponse>> LookupPerson(
        [FromBody] LookupPersonRequest lookupPersonRequest,
        CancellationToken cancellationToken
    )
    {
        Person? person = await _personClient.GetPerson(
            lookupPersonRequest.SocialSecurityNumber,
            lookupPersonRequest.LastName,
            cancellationToken
        );

        return Ok(LookupPersonResponse.CreateFromPerson(person));
    }
}
