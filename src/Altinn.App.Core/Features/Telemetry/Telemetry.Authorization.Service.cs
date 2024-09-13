using System.Diagnostics;
using Altinn.App.Core.Internal.Process.Authorization;
using Altinn.App.Core.Models;
using static Altinn.App.Core.Features.Telemetry.AuthorizationService;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetPartyListActivity(int userId)
    {
        var activity = ActivitySource.StartActivity($"{Prefix}.GetPartyList");
        activity?.SetUserId(userId);
        return activity;
    }

    internal Activity? StartValidateSelectedPartyActivity(int userId, int partyId)
    {
        var activity = ActivitySource.StartActivity($"{Prefix}.ValidateSelectedParty");
        activity?.SetUserId(userId);
        activity?.SetInstanceOwnerPartyId(partyId);
        return activity;
    }

    internal Activity? StartAuthorizeActionActivity(
        InstanceIdentifier instanceIdentifier,
        string action,
        string? taskId = null
    )
    {
        var activity = ActivitySource.StartActivity($"{Prefix}.AuthorizeAction");

        activity?.SetInstanceId(instanceIdentifier.InstanceGuid);
        activity?.SetInstanceOwnerPartyId(instanceIdentifier.InstanceOwnerPartyId);
        activity?.SetTag(InternalLabels.AuthorizationAction, action);
        activity?.SetTaskId(taskId);
        return activity;
    }

    internal Activity? StartAuthorizeActionsActivity(Platform.Storage.Interface.Models.Instance instance)
    {
        var activity = ActivitySource.StartActivity($"{Prefix}.AuthorizeActions");
        activity?.SetInstanceId(instance);

        return activity;
    }

    internal static void AddAuthorizedActionsEvent(
        Activity? activity,
        List<Internal.Process.Elements.UserAction> actions
    )
    {
        if (activity is null)
        {
            return;
        }
        var now = DateTimeOffset.UtcNow;
        ActivityTagsCollection tags = new([new("actions.count", actions.Count),]);
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            tags.Add(new($"actions.{i}.value", action.Id));
            tags.Add(new($"actions.{i}.type", action.ActionType.ToString()));
            tags.Add(new($"actions.{i}.authorized", action.Authorized));
        }

        activity.AddEvent(new ActivityEvent("actions", now, tags));
    }

    internal Activity? StartIsAuthorizerActivity(
        IUserActionAuthorizerProvider authorizer,
        string? taskId,
        string action
    )
    {
        var activity = ActivitySource.StartActivity($"{Prefix}.IsAuthorizerForTaskAndAction");
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
        internal const string Prefix = "Authorization.Service";
    }
}
