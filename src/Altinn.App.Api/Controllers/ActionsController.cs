#nullable enable
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Storage.Interface.Models;
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
public class ActionsController : ControllerBase
{
    private readonly IAuthorizationService _authorization;
    private readonly IInstanceClient _instanceClient;
    private readonly UserActionService _userActionService;

    /// <summary>
    /// Create new instance of the <see cref="ActionsController"/> class
    /// </summary>
    /// <param name="authorization">The authorization service</param>
    /// <param name="instanceClient">The instance client</param>
    /// <param name="userActionService">The user action service</param>
    public ActionsController(IAuthorizationService authorization, IInstanceClient instanceClient, UserActionService userActionService)
    {
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
    [ProducesResponseType(typeof(RedirectResult), 302)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<UserActionResponse>> Perform(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromBody] UserActionRequest actionRequest)
    {
        string? action = actionRequest.Action;
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

        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        if (instance?.Process == null)
        {
            return Conflict($"Process is not started.");
        }

        if (instance.Process.Ended.HasValue)
        {
            return Conflict($"Process is ended.");
        }

        int? userId = HttpContext.User.GetUserIdAsInt();
        if (userId == null)
        {
            return Unauthorized();
        }

        bool authorized = await _authorization.AuthorizeAction(new AppIdentifier(org, app), new InstanceIdentifier(instanceOwnerPartyId, instanceGuid), HttpContext.User, action, instance.Process?.CurrentTask?.ElementId);
        if (!authorized)
        {
            return Forbid();
        }

        UserActionContext userActionContext = new(instance, userId.Value, actionRequest.ButtonId, actionRequest.Metadata);
        IUserAction? actionHandler = _userActionService.GetActionHandler(action);
        if (actionHandler == null)
        {
            return new NotFoundObjectResult(new UserActionResponse()
            {
                Error = new ActionError()
                {
                    Code = "ActionNotFound",
                    Message = $"Action handler with id {action} not found",
                }
            });
        }

        UserActionResult result = await actionHandler.HandleAction(userActionContext);
        if (result.ResultType == ResultType.Redirect)
        {
            return new RedirectResult(result.RedirectUrl ?? throw new ProcessException("Redirect URL missing"));
        }

        if (result.ResultType != ResultType.Success)
        {
            return new BadRequestObjectResult(new UserActionResponse()
            {
                ClientActions = result.ClientActions,
                Error = result.Error
            });
        }

        return new OkObjectResult(new UserActionResponse()
        {
            ClientActions = result.ClientActions,
            UpdatedDataModels = result.UpdatedDataModels
        });
    }
}
