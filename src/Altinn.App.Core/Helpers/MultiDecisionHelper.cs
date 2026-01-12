using System.Globalization;
using System.Security.Claims;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Models;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Constants;
using Altinn.Common.PEP.Helpers;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Helper class for multi decision requests.
/// </summary>
public static class MultiDecisionHelper
{
    private const string XacmlResourceActionId = "urn:oasis:names:tc:xacml:1.0:action:action-id";
    private const string DefaultIssuer = "Altinn";
    private const string DefaultType = "string";
    private const string SubjectId = "s";
    private const string ActionId = "a";
    private const string ResourceId = "r";

    /// <summary>
    /// Creates multi decision request.
    /// </summary>
    [Obsolete("Use overload with InstanceIdentifier instead.")]
    public static XacmlJsonRequestRoot CreateMultiDecisionRequest(
        ClaimsPrincipal user,
        Instance instance,
        List<string> actionTypes
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        return CreateMultiDecisionRequest(
            user,
            new AppIdentifier(instance),
            new InstanceIdentifier(instance),
            actionTypes,
            instance.Process?.CurrentTask?.ElementId,
            instance.Process?.EndEvent
        );
    }

    /// <summary>
    /// Creates multi decision request.
    /// </summary>
    public static XacmlJsonRequestRoot CreateMultiDecisionRequest(
        ClaimsPrincipal user,
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        List<string> actions,
        string? taskId,
        string? endEvent
    )
    {
        XacmlJsonRequest request = new() { AccessSubject = new List<XacmlJsonCategory>() };

        request.AccessSubject.Add(CreateMultipleSubjectCategory(user.Claims));
        request.Action = CreateMultipleActionCategory(actions);
        request.Resource = CreateMultipleResourceCategory(appIdentifier, instanceIdentifier, taskId, endEvent);
        request.MultiRequests = CreateMultiRequestsCategory(request.AccessSubject, request.Action, request.Resource);

        XacmlJsonRequestRoot jsonRequest = new() { Request = request };

        return jsonRequest;
    }

    /// <summary>
    /// Validate a multi decision result and returns a dictionary with the actions and the result.
    /// </summary>
    /// <param name="actions"></param>
    /// <param name="results"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Dictionary<string, bool> ValidatePdpMultiDecision(
        Dictionary<string, bool> actions,
        List<XacmlJsonResult> results,
        ClaimsPrincipal user
    )
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(user);
        foreach (XacmlJsonResult result in results.Where(r => DecisionHelper.ValidateDecisionResult(r, user)))
        {
            foreach (var attributes in result.Category.Select(c => c.Attribute))
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeId == XacmlResourceActionId)
                    {
                        actions[attribute.Value] = true;
                    }
                }
            }
        }

        return actions;
    }

    private static XacmlJsonCategory CreateMultipleSubjectCategory(IEnumerable<Claim> claims)
    {
        XacmlJsonCategory subjectAttributes = DecisionHelper.CreateSubjectCategory(claims);
        subjectAttributes.Id = SubjectId + "1";

        return subjectAttributes;
    }

    private static List<XacmlJsonCategory> CreateMultipleActionCategory(List<string> actionTypes)
    {
        List<XacmlJsonCategory> actionCategories = new();
        int counter = 1;

        foreach (string actionType in actionTypes)
        {
            XacmlJsonCategory actionCategory;
            actionCategory = DecisionHelper.CreateActionCategory(actionType, true);
            actionCategory.Id = ActionId + counter.ToString(CultureInfo.InvariantCulture);
            actionCategories.Add(actionCategory);
            counter++;
        }

        return actionCategories;
    }

    private static List<XacmlJsonCategory> CreateMultipleResourceCategory(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        string? taskId,
        string? endEvent
    )
    {
        List<XacmlJsonCategory> resourcesCategories = new();
        int counter = 1;
        XacmlJsonCategory resourceCategory = new() { Attribute = new List<XacmlJsonAttribute>() };

        if (taskId != null)
        {
            resourceCategory.Attribute.Add(
                DecisionHelper.CreateXacmlJsonAttribute(AltinnUrns.Task, taskId, DefaultType, DefaultIssuer)
            );
        }
        else if (endEvent != null)
        {
            resourceCategory.Attribute.Add(
                DecisionHelper.CreateXacmlJsonAttribute(AltinnUrns.EndEvent, endEvent, DefaultType, DefaultIssuer)
            );
        }

        if (instanceIdentifier.IsNoInstance == false)
        {
            resourceCategory.Attribute.Add(
                DecisionHelper.CreateXacmlJsonAttribute(
                    AltinnXacmlUrns.InstanceId,
                    instanceIdentifier.GetInstanceId(),
                    DefaultType,
                    DefaultIssuer,
                    true
                )
            );
        }

        resourceCategory.Attribute.Add(
            DecisionHelper.CreateXacmlJsonAttribute(
                AltinnXacmlUrns.PartyId,
                instanceIdentifier.InstanceOwnerPartyId.ToString(CultureInfo.InvariantCulture),
                DefaultType,
                DefaultIssuer
            )
        );
        resourceCategory.Attribute.Add(
            DecisionHelper.CreateXacmlJsonAttribute(
                AltinnXacmlUrns.OrgId,
                appIdentifier.Org,
                DefaultType,
                DefaultIssuer
            )
        );
        resourceCategory.Attribute.Add(
            DecisionHelper.CreateXacmlJsonAttribute(
                AltinnXacmlUrns.AppId,
                appIdentifier.App,
                DefaultType,
                DefaultIssuer
            )
        );
        resourceCategory.Id = ResourceId + counter.ToString(CultureInfo.InvariantCulture);
        resourcesCategories.Add(resourceCategory);

        return resourcesCategories;
    }

    private static XacmlJsonMultiRequests CreateMultiRequestsCategory(
        List<XacmlJsonCategory> subjects,
        List<XacmlJsonCategory> actions,
        List<XacmlJsonCategory> resources
    )
    {
        List<string> subjectIds = subjects.Select(s => s.Id).ToList();
        List<string> actionIds = actions.Select(a => a.Id).ToList();
        List<string> resourceIds = resources.Select(r => r.Id).ToList();

        XacmlJsonMultiRequests multiRequests = new()
        {
            RequestReference = CreateRequestReference(subjectIds, actionIds, resourceIds),
        };

        return multiRequests;
    }

    private static List<XacmlJsonRequestReference> CreateRequestReference(
        List<string> subjectIds,
        List<string> actionIds,
        List<string> resourceIds
    )
    {
        List<XacmlJsonRequestReference> references = new();

        foreach (string resourceId in resourceIds)
        {
            foreach (string actionId in actionIds)
            {
                foreach (string subjectId in subjectIds)
                {
                    XacmlJsonRequestReference reference = new();
                    List<string> referenceId = new() { subjectId, actionId, resourceId };
                    reference.ReferenceId = referenceId;
                    references.Add(reference);
                }
            }
        }

        return references;
    }
}
