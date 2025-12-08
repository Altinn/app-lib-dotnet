using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Features.Options.Altinn3LibraryProvider;
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
    private readonly Telemetry? _telemetry;
    private readonly IAppOptionsService _appOptionsService;
    private readonly IAltinn3LibraryCodeListService _altinn3LibraryCodeListService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsController"/> class.
    /// </summary>
    /// <param name="appOptionsService">Service for handling app options</param>
    /// <param name="altinn3LibraryCodeListService">Service for handling Altinn 3 library code lists.</param>
    /// <param name="telemetry">The telemetry client.</param>
    public OptionsController(
        IAppOptionsService appOptionsService,
        IAltinn3LibraryCodeListService altinn3LibraryCodeListService,
        Telemetry? telemetry = null
    )
    {
        _appOptionsService = appOptionsService;
        _altinn3LibraryCodeListService = altinn3LibraryCodeListService;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Api that exposes app related options
    /// </summary>
    /// <remarks>The Tags field is only populated when requesting library code lists.</remarks>
    /// <param name="creatorOrg">The organization that created the code list</param>
    /// <param name="codeListId">Code list id, required if creator org is provided</param>
    /// <param name="version">Code list version, only used in combination with creator org and code list id, defaults to latest if not provided</param>
    /// <param name="language">The language selected by the user, ISO 639-1 (eg. nb)</param>
    /// <returns>The options list.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{creatorOrg}/{codeListId}")]
    public async Task<IActionResult> Get(
        [FromRoute] string creatorOrg,
        [FromRoute] string codeListId,
        [FromQuery] string? version = "latest",
        [FromQuery] string? language = null
    )
    {
        using var telemetry = _telemetry?.StartGetOptionsActivity();
        var altinn3LibraryCodeListResponse = await _altinn3LibraryCodeListService.GetCachedCodeListResponseAsync(
            creatorOrg,
            codeListId,
            version,
            HttpContext.RequestAborted
        );

        var appOptions = _altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, language);

        HttpContext.Response.Headers.Append(
            "Altinn-DownstreamParameters",
            appOptions.Parameters.ToUrlEncodedNameValueString(',')
        );

        return Ok(appOptions.Options);
    }

    /// <summary>
    /// Api that exposes app related options
    /// </summary>
    /// <remarks>The Tags field is only populated when requesting library code lists.</remarks>
    /// <param name="optionsId">The optionsId configured for the options provider in the app startup.</param>
    /// <param name="queryParams">Query parameters supplied</param>
    /// <param name="language">The language selected by the user (ISO 639-1, e.g., 'nb').</param>
    /// <returns>The options list.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{optionsId}")]
    public async Task<IActionResult> Get(
        [FromRoute] string optionsId,
        [FromQuery] Dictionary<string, string> queryParams,
        [FromQuery] string? language = null
    )
    {
        var appOptions = await _appOptionsService.GetOptionsAsync(optionsId, language, queryParams);
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

        var appOptions = await _appOptionsService.GetOptionsAsync(
            instanceIdentifier,
            optionsId,
            language ?? LanguageConst.Nb,
            queryParams
        );

        // Only return NotFound if we can't find an options provider.
        // If we find the options provider, but it doesnt' have values, return empty list.
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
}
