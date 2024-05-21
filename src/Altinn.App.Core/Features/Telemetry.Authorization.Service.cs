using System.Diagnostics;
using Altinn.App.Core.Internal.Process.Authorization;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using static Altinn.App.Core.Features.Telemetry.AuthorizationService;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetPartyListActivity(int userId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetPartyList");
        if (activity is not null)
        {
            activity.SetTag(InternalLabels.AuthorizationUserId, userId);
        }
        return activity;
    }

    internal Activity? StartValidateSelectedPartyActivity(int userId, int partyId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.ValidateSelectedParty");
        if (activity is not null)
        {
            activity.SetTag(InternalLabels.AuthorizationUserId, userId);
            activity.SetInstanceOwnerPartyId(partyId);
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

    internal Activity? StartAuthorizeActionsActivity(
        Platform.Storage.Interface.Models.Instance instance,
        List<AltinnAction> actions
    )
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.AuthorizeActions");
        if (activity is not null)
        {
            activity.SetInstanceId(instance);
            string actionTypes = string.Join(", ", actions.Select(a => a.Value.ToString()));
            activity.SetTag(InternalLabels.AuthorizationActionId, actionTypes);
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
            activity.SetTaskId(taskId);
            if (authorizer.TaskId is not null)
                activity.SetTag(InternalLabels.AuthorizerTaskId, authorizer.TaskId);
            if (authorizer.Action is not null)
                activity.SetTag(InternalLabels.AuthorizerAction, authorizer.Action);
            activity.SetTag(InternalLabels.AuthorizationAction, action);
        }
        return activity;
    }

    internal static class AuthorizationService
    {
        internal const string _prefix = "Authorization.Service";
    }
}