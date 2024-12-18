using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.AccessManagement.Exceptions;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

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
    IDelegationBuilderApplicationId WithApplicationId(string applicationId);
}

internal interface IDelegationBuilderApplicationId
{
    IDelegationBuilderInstanceId WithInstanceId(string instanceId);
}

internal interface IDelegationBuilderInstanceId
{
    IDelegationBuilderDelegator WithDelegator(Delegator delegator);
}

internal interface IDelegationBuilderDelegator
{
    IDelegationBuilderRecipient WithDelegatee(Delegatee recipient);
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
    private Delegator? _delegator;
    private Delegatee? _recipient;
    private List<RightRequest>? _rights;

    private DelegationBuilder() { }

    public static IDelegationBuilderStart Create() => new DelegationBuilder();

    public IDelegationBuilderApplicationId WithApplicationId(string applicationId)
    {
        NotNullOrEmpty(applicationId, nameof(applicationId));
        _applicationId = AppIdHelper.TryGetResourceId(applicationId, out AppResourceId? resourceId)
            ? resourceId.Value
            : throw new ArgumentException($"Invalid application id: {applicationId}", nameof(applicationId));
        return this;
    }

    public IDelegationBuilderInstanceId WithInstanceId(string instanceId)
    {
        NotNullOrEmpty(instanceId, nameof(instanceId));
        _instanceId = instanceId;
        return this;
    }

    public IDelegationBuilderDelegator WithDelegator(Delegator delegator)
    {
        NotNullOrEmpty(delegator, nameof(delegator));
        _delegator = delegator;
        return this;
    }

    public IDelegationBuilderRecipient WithDelegatee(Delegatee recipient)
    {
        NotNullOrEmpty(recipient, nameof(recipient));
        _recipient = recipient;
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
        NotNullOrEmpty(_recipient, nameof(_recipient));
        NotNullOrEmpty(_rights, nameof(_rights));

        return new DelegationRequest
        {
            ResourceId = _applicationId,
            InstanceId = _instanceId,
            From = _delegator,
            To = _recipient,
            Rights = _rights,
        };
    }
}
