using System.Net.Mime;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Mappers;
using Altinn.App.Api.Models;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// This controller class provides Folkeregisteret (DSF) person search functionality.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/person-search")]
public class PersonSearchControllerController : ControllerBase
{
    private readonly IPersonClient _personClient;

    /// <summary>
    /// Initialize a new instance of <see cref="PersonSearchControllerController"/> with the given services.
    /// </summary>
    /// <param name="personClient">A client for searching for a person.</param>
    public PersonSearchControllerController(IPersonClient personClient)
    {
        _personClient = personClient;
    }

    /// <summary>
    /// Allows searching for a person in Folkeregisteret (DSF)
    /// </summary>
    /// <param name="personSearchRequest">Payload that contains params for executing a person search.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="PersonSearchResponse"/> object.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonSearchResponse>> SearchForPerson(
        [FromBody] PersonSearchRequest personSearchRequest,
        CancellationToken cancellationToken
    )
    {
        Person? person = await _personClient.GetPerson(
            personSearchRequest.SocialSecurityNumber,
            personSearchRequest.LastName,
            cancellationToken
        );

        return person is null ? NotFound() : PersonMapper.MapToDto(person);
    }
}
