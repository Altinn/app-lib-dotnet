using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Internal.AccessManagement.Exceptions;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.AccessManagement.Builders;

internal abstract class DelegationBuilderBase
{
    internal static void NotNullOrEmpty([NotNull] object? value, string? errorMessage = null)
    {
        if (
            value is null
            || value is string str && string.IsNullOrWhiteSpace(str)
            || value is ReadOnlyMemory<byte> { IsEmpty: true }
        )
        {
            throw new AccessManagementArgumentException(errorMessage); // TODO: add custom exception
        }
    }
}

internal interface IDelegationBuilderStart
{
    IDelegationBuilderApplicationId WithApplicationId(AppIdentifier appIdentifier);
}

internal interface IDelegationBuilderApplicationId
{
    IDelegationBuilderInstanceId WithInstanceId(string instanceId);
}

internal interface IDelegationBuilderInstanceId
{
    IDelegationBuilderDelegator WithDelegator(DelegationParty delegator);
}

internal interface IDelegationBuilderDelegator
{
    IDelegationBuilderRecipient WithDelegatee(DelegationParty delegatee);
}

internal interface IDelegationBuilderRecipient
{
    IDelegationBuilder WithRight(RightRequest rightRequest);
    IDelegationBuilder WithRights(List<RightRequest> rightRequests);
}

internal interface IDelegationBuilder
    : IDelegationBuilderStart,
        IDelegationBuilderApplicationId,
        IDelegationBuilderInstanceId,
        IDelegationBuilderDelegator,
        IDelegationBuilderRecipient
{
    DelegationRequest Build();
}

internal sealed class DelegationBuilder : DelegationBuilderBase, IDelegationBuilder
{
    private string? _applicationId;
    private string? _instanceId;
    private DelegationParty? _delegator;
    private DelegationParty? _delegatee;
    private List<RightRequest>? _rights;

    private DelegationBuilder() { }

    public static IDelegationBuilderStart Create() => new DelegationBuilder();

    public IDelegationBuilderApplicationId WithApplicationId(AppIdentifier appIdentifier)
    {
        AppResourceId appResourceId = AppResourceId.FromAppIdentifier(appIdentifier);
        _applicationId = appResourceId.Value;
        return this;
    }

    public IDelegationBuilderInstanceId WithInstanceId(string instanceId)
    {
        NotNullOrEmpty(instanceId, nameof(instanceId));
        _instanceId = instanceId;
        return this;
    }

    public IDelegationBuilderDelegator WithDelegator(DelegationParty delegator)
    {
        NotNullOrEmpty(delegator, nameof(delegator));
        _delegator = delegator;
        return this;
    }

    public IDelegationBuilderRecipient WithDelegatee(DelegationParty delegatee)
    {
        NotNullOrEmpty(delegatee, nameof(delegatee));
        _delegatee = delegatee;
        return this;
    }

    public IDelegationBuilder WithRight(RightRequest rightRequest)
    {
        _rights = [rightRequest];
        return this;
    }

    public IDelegationBuilder WithRights(List<RightRequest> rightRequests)
    {
        _rights = [.. _rights ?? [], .. rightRequests];
        return this;
    }

    public IDelegationBuilder WithRight(AccessRightBuilder rightBuilder)
    {
        _rights = [rightBuilder.Build()];
        return this;
    }

    public DelegationRequest Build()
    {
        NotNullOrEmpty(_applicationId, nameof(_applicationId));
        NotNullOrEmpty(_instanceId, nameof(_instanceId));
        NotNullOrEmpty(_delegator, nameof(_delegator));
        NotNullOrEmpty(_delegatee, nameof(_delegatee));
        NotNullOrEmpty(_rights, nameof(_rights));

        return new DelegationRequest
        {
            ResourceId = _applicationId,
            InstanceId = _instanceId,
            From = _delegator,
            To = _delegatee,
            Rights = _rights,
        };
    }
}
