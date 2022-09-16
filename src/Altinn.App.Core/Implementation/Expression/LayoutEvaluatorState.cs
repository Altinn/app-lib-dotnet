using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Implementation.Expression;

public class LayoutEvaluatorState
{
    private readonly IDataModelAccessor _dataModel;
    private readonly ComponentModel _componentModel;
    private readonly FrontEndSettings _frontEndSettings;
    private readonly Instance _instanceContext;

    public LayoutEvaluatorState(IDataModelAccessor dataModel, ComponentModel componentModel, FrontEndSettings frontEndSettings, Instance instance)
    {
        _dataModel = dataModel;
        _componentModel = componentModel;
        _frontEndSettings = frontEndSettings;
        _instanceContext = instance;
        // TODO: shaddowFields ...
    }

    private IEnumerable<ComponentContext> GetComponentContextsReucrs(Component component, IEnumerable<int> indexes)
    {
        yield return new ComponentContext(component, indexes.Any() ? indexes.ToArray() : null);
        if ((component.Children?.Any() ?? false) && (component.GetModelBinding("group") is not null))
        {
            var rowLength = _dataModel!.GetModelDataCount(component.GetModelBinding("group")!, indexes.ToArray()) ?? 0;
            foreach (var index in Enumerable.Range(0, rowLength))
            {
                foreach (var child in component.Children)
                {
                    var subIndexes = indexes.ToList();
                    subIndexes.Add(index);
                    foreach (var context in GetComponentContextsReucrs(child, subIndexes))
                    {
                        yield return context;
                    }
                }
            }
        }
    }

    public IEnumerable<ComponentContext> GetComponentContexts()
    {
        if (_componentModel is null || _dataModel is null)
        {
            return Enumerable.Empty<ComponentContext>();
        }


        var ret = new List<ComponentContext>();
        foreach (var page in _componentModel.Pages.Values)
        {
            foreach (var component in page.Components)
            {
                ret.AddRange(GetComponentContextsReucrs(component, Enumerable.Empty<int>()));
            }
        }

        return ret;
    }


    public static Dictionary<string, string>? GetModelBindings(JsonElement component)
    {
        if (component.ValueKind == JsonValueKind.Object &&
             component.TryGetProperty("dataModelBindings", out var dataModelBindings) &&
             dataModelBindings.ValueKind == JsonValueKind.Object)
        {
            return dataModelBindings
                    .EnumerateObject()
                    .Where(j => j.Value.ValueKind == JsonValueKind.String)
                    .ToDictionary(j => j.Name, j => j.Value.GetString()!);
        }
        return null;
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

    public object? GetApplicationSetting(string key)
    {
        return (_frontEndSettings?.TryGetValue(key, out var value) ?? false) ? value : null;
    }

    public string AddInidicies(string binding, ComponentContext context)
    {
        return _dataModel.AddIndicies(binding, context.RowIndices);
    }
}