#nullable enable
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Altinn.App.Core.Internal.Auth.IAuthorizationService;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller that handles actions performed by users
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/actions")]
public class ActionsController: ControllerBase
{
    private readonly ILogger<ProcessController> _logger;
    private readonly IAuthorizationService _authorization;
    private readonly IInstanceClient _instanceClient;
    private readonly IUserActionService _userActionService;

    /// <summary>
    /// Create new instance of the <see cref="ActionsController"/> class
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/> used for logging errors</param>
    /// <param name="authorization">The authorization service</param>
    /// <param name="instanceClient">The instance client</param>
    /// <param name="userActionService">The user action service</param>
    public ActionsController(ILogger<ProcessController> logger, IAuthorizationService authorization, IInstanceClient instanceClient, IUserActionService userActionService)
    {
        _logger = logger;
        _authorization = authorization;
        _instanceClient = instanceClient;
        _userActionService = userActionService;
    }
    
    /// <summary>
    /// Perform a task action on an instance
    /// </summary>
    /// <param name="org">unique identfier of the organisation responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that this the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="actionRequest">user action request</param>
    /// <returns><see cref="UserActionResponse"/></returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(UserActionResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<UserActionResponse>> Perform(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromBody] UserActionRequest actionRequest)
    {
        var action = actionRequest.Action;
        if (action == null)
        {
            return new BadRequestObjectResult(new ProblemDetails()
            {
                Instance = instanceGuid.ToString(),
                Status = 400,
                Title = "Action is missing",
                Detail = "Action is missing in the request"
            });
        }
        
        var instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        
        if (instance?.Process == null)
        {
            return Conflict($"Process is not started.");
        }
        
        if (instance.Process.Ended.HasValue)
        {
            return Conflict($"Process is ended.");
        }
        
        var userId = HttpContext.User.GetUserIdAsInt();
        if (userId == null)
        {
            return Unauthorized();
        }
        
        var authorized = await _authorization.AuthorizeAction(new AppIdentifier(org, app), new InstanceIdentifier(instanceOwnerPartyId, instanceGuid), HttpContext.User, action, instance.Process?.CurrentTask?.ElementId);
        if (!authorized)
        {
            return Forbid();
        }

        UserActionContext userActionContext = new UserActionContext(instance, userId.Value, actionRequest.ButtonId, actionRequest.Metadata);
        
        var result = await _userActionService.HandleAction(userActionContext, action);

        if (!result.Success)
        {
            Response.StatusCode = HttpContext.Response.StatusCode = 400;
            return new UserActionResponse()
            {
                FrontendActions = result.FrontendActions,
                ValidationIssues = result.ValidationIssues
            };
        }

        return new OkObjectResult(new UserActionResponse()
        {
            FrontendActions = result.FrontendActions,
            ValidationIssues = result.ValidationIssues,
            UpdatedDataModels = result.UpdatedDataModels
        });
    }
}