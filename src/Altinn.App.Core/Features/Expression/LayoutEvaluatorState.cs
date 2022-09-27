using System;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Expression;

/// <summary>
/// Collection class to hold all the shared state that is required for evaluating expressions in a layout.
/// </summary>
public class LayoutEvaluatorState
{
    private readonly IDataModelAccessor _dataModel;
    private readonly ComponentModel _componentModel;
    private readonly FrontEndSettings _frontEndSettings;
    private readonly Instance _instanceContext;

    /// <summary>
    /// Constructor for LayoutEvaluatorState. Usually called via <see cref="LayoutEvaluatorStateInitializer" /> that can be fetched from dependency injection.
    /// </summary>
    public LayoutEvaluatorState(IDataModelAccessor dataModel, ComponentModel componentModel, FrontEndSettings frontEndSettings, Instance instance)
    {
        _dataModel = dataModel;
        _componentModel = componentModel;
        _frontEndSettings = frontEndSettings;
        _instanceContext = instance;
        // TODO: shaddowFields ...
    }


    /// <summary>
    /// Get a hierarcy of the different contexts in the component model (remember to iterate <see cref="ComponentContext.ChildContexts" />)
    /// </summary>
    public IEnumerable<ComponentContext> GetComponentContexts()
    {
        if (_componentModel is null || _dataModel is null)
        {
            throw new InvalidOperationException("Layout evaluator state is not properly initialized");
        }

        return _componentModel.Pages.Values.Select((
            (page) => new ComponentContext
            (
                page,
                null,
                page.Children.Select(c => GetComponentContextsRecurs(c, _dataModel, new int[] { })).ToArray()
            )
        )).ToArray();
    }

    private static ComponentContext GetComponentContextsRecurs(Component component, IDataModelAccessor dataModel, int[] indexes)
    {
        if (component.Children?.Any() ?? false)
        {
            var children = new List<ComponentContext>();

            if (component.DataModelBindings.TryGetValue("group", out var groupBinding))
            {
                var rowLength = dataModel.GetModelDataCount(groupBinding, indexes.ToArray()) ?? 0;
                foreach (var index in Enumerable.Range(0, rowLength))
                {
                    foreach (var child in component.Children)
                    {
                        // concatenate [...indexes, index]
                        var subIndexes = new int[indexes.Length + 1];
                        indexes.CopyTo(subIndexes.AsSpan());
                        subIndexes[^1] = index;

                        children.Add(GetComponentContextsRecurs(child, dataModel, subIndexes));
                    }
                }
            }
            else
            {
                foreach (var child in component.Children)
                {
                    children.Add(GetComponentContextsRecurs(child, dataModel, indexes));
                }
            }

            return new ComponentContext(component, indexes.Length == 0 ? null : indexes.ToArray(), children);
        }

        return new ComponentContext(component, indexes.Length == 0 ? null : indexes.ToArray(), Enumerable.Empty<ComponentContext>());
    }

    public string? GetFrontendSetting(string key)
    {
        return _frontEndSettings.TryGetValue(key, out var setting) ? setting : null;
    }

    public object? GetModelData(string key, ComponentContext? context = null)
    {
        return _dataModel.GetModelData(key, context?.RowIndices);
    }

    public void RemoveDataField(string key)
    {
        _dataModel.RemoveField(key);
    }

    public object? GetComponentData(string key, ComponentContext context)
    {
        return _componentModel.GetComponentData(key, context, _dataModel);
    }

    public string GetInstanceContext(string key)
    {
        // Instance context only supports a small subset of variables from the instance
        return key switch
        {
            "instanceOwnerPartyId" => _instanceContext.InstanceOwner.PartyId,
            "appId" => _instanceContext.AppId,
            "instanceId" => _instanceContext.Id,
            _ => throw new Exception($"Unknown Instance context property {key}"),
        };
    }

    public string AddInidicies(string binding, ComponentContext context)
    {
        return _dataModel.AddIndicies(binding, context.RowIndices);
    }
}