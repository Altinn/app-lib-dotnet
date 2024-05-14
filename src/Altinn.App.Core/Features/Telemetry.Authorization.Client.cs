using System.Diagnostics;
using Altinn.App.Core.Internal.Process.Authorization;
using Altinn.App.Core.Models;
using static Altinn.App.Core.Features.Telemetry.AuthorizationClient;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartClientGetPartyListActivity(int userId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetPartyList");
        if (activity is not null)
        {
            activity.SetTag(InternalLabels.AuthorizationUserId, userId);
        }
        return activity;
    }

    internal Activity? StartClientValidateSelectedPartyActivity(int userId, int partyId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateSelectedParty");
        if (activity is not null)
        {
            activity.SetTag(InternalLabels.AuthorizationUserId, userId);
            activity.SetInstanceOwnerPartyId(partyId);
        }
        return activity;
    }

    internal Activity? StartClientAuthorizeActionActivity(
        InstanceIdentifier instanceIdentifier,
        string action,
        string? taskId = null
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.AuthorizeAction");
        if (activity is not null)
        {
            activity.SetInstanceId(instanceIdentifier.InstanceGuid);
            activity.SetInstanceOwnerPartyId(instanceIdentifier.InstanceOwnerPartyId);
            activity.SetTag(InternalLabels.AuthorizationAction, action);
            if (taskId is not null)
            {
                activity.SetTag(Labels.TaskId, taskId);
            }
        }
        return activity;
    }

    internal Activity? StartClientAuthorizeActionsActivity(
        Platform.Storage.Interface.Models.Instance instance,
        List<string> actionIds
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.AuthorizeActions");
        if (activity is not null)
        {
            activity.SetInstanceId(instance);
            string actionTypes = string.Join(", ", actionIds);
            activity.SetTag(InternalLabels.AuthorizationActionId, actionTypes);
        }
        return activity;
    }

    internal Activity? StartClientIsAuthorizerActivity(
        IUserActionAuthorizerProvider authorizer,
        string? taskId,
        string action
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.IsAuthorizerForTaskAndAction");
        if (activity is not null)
        {
            activity.SetTaskId(taskId);
            if (authorizer.TaskId is not null)
                activity.SetTag(InternalLabels.AuthorizerTaskId, authorizer.TaskId);
            if (authorizer.Action is not null)
                activity.SetTag(InternalLabels.AuthorizerAction, authorizer.Action);
            activity.SetTag(InternalLabels.AuthorizationAction, action);
        }
        return activity;
    }

    internal static class AuthorizationClient
    {
        internal const string _prefix = "Authorization.Client";
    }
}
