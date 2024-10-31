using Altinn.App.Core.Internal.AccessManagement.Models;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IApplicationIdStep
{
    IInstanceIdStep WithAppResourceId(string applicationId);
}

internal interface IInstanceIdStep
{
    IDelegatorStep WithInstanceId(string instanceId);
}

internal interface IDelegatorStep
{
    IRecipientStep WithDelegator(From delegator);
}

internal interface IRecipientStep
{
    IRightStep WithRecipient(To recipient);
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

internal sealed class DelegationRequestBuilder
    : IApplicationIdStep,
        IInstanceIdStep,
        IDelegatorStep,
        IRecipientStep,
        IRightStep,
        IDelegationCreateStep
{
    private DelegationRequest _delegation = new DelegationRequest { Rights = [] };

    public DelegationRequestBuilder()
    {
        _delegation = new DelegationRequest();
    }

    public static IApplicationIdStep CreateBuilder() => new DelegationRequestBuilder();

    public IInstanceIdStep WithAppResourceId(string applicationId)
    {
        _delegation.ResourceId = applicationId;
        return this;
    }

    public IDelegatorStep WithInstanceId(string instanceId)
    {
        _delegation.InstanceId = instanceId;
        return this;
    }

    public IRecipientStep WithDelegator(From delegator)
    {
        _delegation.From = delegator;
        return this;
    }

    public IRightStep WithRecipient(To recipient)
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
        DelegationRequest delegation = _delegation;
        _delegation = new DelegationRequest();
        return delegation;
    }

    internal sealed class RightBuilder : IRightBuilder
    {
        private readonly IRightStep _parentBuilder;
        private readonly Right _right = new Right { Resource = new List<Resource>() };

        public RightBuilder(IRightStep parentBuilder)
        {
            _parentBuilder = parentBuilder;
        }

        public IRightBuilder WithAction(string type, string value)
        {
            _right.Action = new Models.Action { Type = type, Value = value };
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
