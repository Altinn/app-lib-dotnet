using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Represents the Options API.
/// </summary>
[Route("{org}/{app}/api/options")]
[ApiController]
public class OptionsController : ControllerBase
{
    private readonly IAppOptionsService _appOptionsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsController"/> class.
    /// </summary>
    /// <param name="appOptionsService">Service for handling app options</param>
    public OptionsController(IAppOptionsService appOptionsService)
    {
        _appOptionsService = appOptionsService;
    }

    /// <summary>
    /// Api that exposes app related options
    /// </summary>
    /// <param name="optionsId">The optionsId</param>
    /// <param name="queryParams">Query parameters supplied</param>
    /// <param name="language">The language selected by the user.</param>
    /// <returns>The options list</returns>
    [HttpGet("{optionsId}")]
    public async Task<IActionResult> Get(
        [FromRoute] string optionsId,
        [FromQuery] Dictionary<string, string> queryParams,
        [FromQuery] string? language = null
    )
    {
        AppOptions appOptions = await _appOptionsService.GetOptionsAsync(optionsId, language, queryParams);
        if (appOptions?.Options == null)
        {
            return NotFound();
        }

        HttpContext.Response.Headers.Append(
            "Altinn-DownstreamParameters",
            appOptions.Parameters.ToUrlEncodedNameValueString(',')
        );

        return Ok(appOptions.Options);
    }

    /// <summary>
    /// API endpoint for fetching Altinn 3 library code lists
    /// </summary>
    /// <param name="creatorOrg">The organization that initially created the code list</param>
    /// <param name="codeListId">The code list id</param>
    /// <param name="version">The code list version, defaults to latest if not provided</param>
    /// <param name="language">The language requested, ISO 639-1 (eg. nb)</param>
    /// <param name="tags"></param>
    /// <returns>The code list</returns>
    [HttpGet("{creatorOrg}/{codeListId}")]
    public async Task<IActionResult> GetCodelist(
        [FromRoute] string creatorOrg,
        [FromRoute] string codeListId,
        [FromQuery] string? version = null,
        [FromQuery] string? language = null
    )
    // [FromQuery] string? tags = null
    {
        var orgLibraryAppOptions = await _appOptionsService.GetCachableCodeListResponseAsync(
            creatorOrg,
            codeListId,
            version
        );

        return Ok(_appOptionsService.MapOrgLibraryAppOptions(orgLibraryAppOptions, language));
    }

    /// <summary>
    /// Exposes options related to the app and logged in user
    /// </summary>
    /// <param name="org">unique identifier of the organisation responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that is the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="optionsId">The optionsId</param>
    /// <param name="language">The language selected by the user.</param>
    /// <param name="queryParams">Query parameteres supplied</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_READ)]
    [Route("/{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/options/{optionsId}")]
    public async Task<IActionResult> Get(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string optionsId,
        [FromQuery] string? language,
        [FromQuery] Dictionary<string, string> queryParams
    )
    {
        var instanceIdentifier = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        AppOptions appOptions = await _appOptionsService.GetOptionsAsync(
            instanceIdentifier,
            optionsId,
            language ?? LanguageConst.Nb,
            queryParams
        );

        // Only return NotFound if we can't find an options provider.
        // If we find the options provider, but it doesnt' have values, return empty list.
        if (appOptions.Options == null)
        {
            return NotFound();
        }

        HttpContext.Response.Headers.Append(
            "Altinn-DownstreamParameters",
            appOptions.Parameters.ToUrlEncodedNameValueString(',')
        );

        return Ok(appOptions.Options);
    }
}
