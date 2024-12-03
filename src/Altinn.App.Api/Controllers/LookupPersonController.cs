using System.Net;
using System.Net.Mime;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models.Result;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LookupPersonResponse>> LookupPerson(
        [FromBody] LookupPersonRequest lookupPersonRequest,
        CancellationToken cancellationToken
    )
    {
        var personResult = await GetPersoonDataOrError(
            lookupPersonRequest.SocialSecurityNumber,
            lookupPersonRequest.LastName,
            cancellationToken
        );

        if (!personResult.Success)
        {
            ProblemDetails problemDetails = personResult.Error;
            return StatusCode(problemDetails.Status ?? 500, problemDetails);
        }

        return Ok(LookupPersonResponse.CreateFromPerson(personResult.Ok));
    }

    private async Task<ServiceResult<Person, ProblemDetails>> GetPersoonDataOrError(
        string ssn,
        string lastName,
        CancellationToken cancellationToken
    )
    {
        Person? person;
        try
        {
            person = await _personClient.GetPerson(ssn, lastName, cancellationToken);
        }
        catch (PlatformHttpException e)
        {
            if (e.Response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Access to the register is forbidden",
                    Status = StatusCodes.Status403Forbidden,
                };
            }
            else if (e.Response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new ProblemDetails
                {
                    Title = "Too many requests",
                    Detail = "Too many requests to the register",
                    Status = StatusCodes.Status429TooManyRequests,
                };
            }
            return new ProblemDetails
            {
                Title = "Error when calling Register",
                Detail = e.Message,
                Status = StatusCodes.Status500InternalServerError,
            };
        }

        if (person is null)
        {
            return new ProblemDetails
            {
                Title = "Person not found",
                Detail = $"No person is registered with this combination of national ID number/D-number and name",
                Status = StatusCodes.Status400BadRequest,
            };
        }

        return person;
    }
}
