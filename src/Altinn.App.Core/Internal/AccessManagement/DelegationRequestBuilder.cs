using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IDelegatorStep
{
    IRecipientStep WithDelegator(Delegator delegator);
}

internal interface IRecipientStep
{
    IRightStep WithRecipient(Delegatee recipient);
}

internal interface IRightStep : IDelegationCreateStep
{
    IRightBuilder AddRight();
}

internal interface IDelegationCreateStep
{
    DelegationRequest Build();
}

internal interface IRightBuilder
{
    IRightBuilder WithAction(string type, string value);
    IRightBuilder AddResource(string type, string value);
    IRightStep BuildRight();
}

internal sealed class DelegationRequestBuilder : IDelegatorStep, IRecipientStep, IRightStep, IDelegationCreateStep
{
    private DelegationRequest _delegation;

    public DelegationRequestBuilder(string applicationId, string instanceId)
    {
        _delegation = new DelegationRequest() { ResourceId = applicationId, InstanceId = instanceId };
    }

    public static IDelegatorStep CreateBuilder(string applicationId, string instanceId) =>
        new DelegationRequestBuilder(applicationId, instanceId);

    public IRecipientStep WithDelegator(Delegator delegator)
    {
        _delegation.From = delegator;
        return this;
    }

    public IRightStep WithRecipient(Delegatee recipient)
    {
        _delegation.To = recipient;
        return this;
    }

    public IRightBuilder AddRight()
    {
        return new RightBuilder(this);
    }

    public DelegationRequest Build()
    {
        return _delegation;
    }

    internal sealed class RightBuilder : IRightBuilder
    {
        private readonly IRightStep _parentBuilder;
        private readonly RightRequest _right = new RightRequest { Resource = new List<Resource>() };

        public RightBuilder(IRightStep parentBuilder)
        {
            _parentBuilder = parentBuilder;
        }

        public IRightBuilder WithAction(string type, string value)
        {
            _right.Action = new AltinnAction { Type = type, Value = value };
            return this;
        }

        public IRightBuilder AddResource(string type, string value)
        {
            _right.Resource.Add(new Resource { Type = type, Value = value });
            return this;
        }

        public IRightStep BuildRight()
        {
            ((DelegationRequestBuilder)_parentBuilder)._delegation.Rights.Add(_right);
            return _parentBuilder;
        }
    }
}
