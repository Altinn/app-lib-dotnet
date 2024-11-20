using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement.Builders;

internal interface IAccessRightBuilderStart
{
    IAccessRightBuilderAction WithAction(string type, string value);
}

internal interface IAccessRightBuilderAction
{
    IAccessRightBuilder WithResource(string type, string value);
    IAccessRightBuilder WithResources(List<Resource> resources);
}

internal interface IAccessRightBuilder : IAccessRightBuilderStart, IAccessRightBuilderAction
{
    RightRequest Build();
}

internal sealed class AccessRightBuilder : IAccessRightBuilder
{
    private AltinnAction? _action;
    private List<Resource>? _resources;

    private AccessRightBuilder() { }

    public static IAccessRightBuilderStart Create() => new AccessRightBuilder();

    public IAccessRightBuilderAction WithAction(string type, string value)
    {
        _action = new AltinnAction { Type = type, Value = value };
        return this;
    }

    public IAccessRightBuilder WithResource(string type, string value)
    {
        _resources = [new Resource { Type = type, Value = value }];

        return this;
    }

    public IAccessRightBuilder WithResources(List<Resource> resources)
    {
        _resources = [.. _resources ?? [], .. resources];
        return this;
    }

    public RightRequest Build()
    {
        return new RightRequest { Action = _action, Resource = _resources ?? [] };
    }
}
