using System.Diagnostics;
using Altinn.App.Core.Internal.Process.Authorization;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using static Altinn.App.Core.Features.Telemetry.Authorization;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetPartyListActivity(int userId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetPartyList");
        if (activity is not null)
        {
            activity.SetTag(AuthorizationLabels.UserId, userId);
        }
        return activity;
    }

    internal Activity? StartValidateSelectedPartyActivity(int userId, int partyId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateSelectedParty");
        if (activity is not null)
        {
            activity.SetTag(AuthorizationLabels.UserId, userId);
            activity.SetTag(Labels.InstanceOwnerPartyId, partyId); // TODO: verify that this party id is indeed the instance owner party id
        }
        return activity;
    }

    internal Activity? StartAuthorizeActionActivity(
        InstanceIdentifier instanceIdentifier,
        string action,
        string? taskId = null
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.AuthorizeAction");
        if (activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceIdentifier.InstanceGuid.ToString());
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceIdentifier.InstanceOwnerPartyId.ToString());
            activity.SetTag(AuthorizationLabels.Action, action);
            if (taskId is not null)
            {
                activity.SetTag(Labels.TaskId, taskId);
            }
        }
        return activity;
    }

    internal Activity? StartAuthorizeActionsActivity(
        Platform.Storage.Interface.Models.Instance instance,
        List<AltinnAction> actions
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.AuthorizeActions");
        if (activity is not null)
        {
            Guid InstanceGuid = Guid.Parse(instance.Id.Split('/')[1]);
            activity.SetTag(Labels.InstanceGuid, InstanceGuid);

            string actionTypes = string.Join(", ", actions.Select(a => a.Value.ToString()));
            activity.SetTag(AuthorizationLabels.ActionId, actionTypes);
        }
        return activity;
    }

    internal Activity? StartIsAuthorizerActivity(
        IUserActionAuthorizerProvider authorizer,
        string? taskId,
        string action
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.IsAuthorizerForTaskAndAction");
        if (activity is not null)
        {
            if (taskId is not null)
                activity.SetTag(Labels.TaskId, taskId);
            if (authorizer.TaskId is not null)
                activity.SetTag(AuthorizationLabels.AuthorizerTaskId, authorizer.TaskId);
            if (authorizer.Action is not null)
                activity.SetTag(AuthorizationLabels.AuthorizerAction, authorizer.Action);
            activity.SetTag(AuthorizationLabels.Action, action);
        }
        return activity;
    }

    internal static class Authorization
    {
        internal const string _prefix = "Authorization";
    }

    internal static class AuthorizationLabels
    {
        internal const string UserId = "authorization.userid";
        internal const string Action = "authorization.action";
        internal const string ActionId = "authorization.actionid";
        internal const string AuthorizerAction = "authorization.authorizer.action";
        internal const string AuthorizerTaskId = "authorization.authorizer.taskid";
    }
}
