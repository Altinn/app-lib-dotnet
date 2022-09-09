using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Implementation.Expression;

public interface IDataModelAccessor
{
    object? GetModelData(string key, ReadOnlySpan<int> indicies = default);

    int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default);
}

public class JsonDataModel : IDataModelAccessor
{
    private readonly JsonElement? _modelRoot;
    public JsonDataModel(JsonElement? modelRoot)
    {
        _modelRoot = modelRoot;
    }


    /// <inheritdoc />
    public object? GetModelData(string key, ReadOnlySpan<int> indicies = default)
    {
        if (_modelRoot is null)
        {
            return null;
        }

        return GetModelDataRecursive(key.Split('.'), 0, _modelRoot.Value, indicies);
    }


    private object? GetModelDataRecursive(string[] keys, int index, JsonElement currentModel, ReadOnlySpan<int> indicies)
    {
        if (index == keys.Length)
        {
            return currentModel.ValueKind switch
            {
                JsonValueKind.String => currentModel.GetString(),
                JsonValueKind.Number => currentModel.GetDouble(),
                JsonValueKind.Object => null, //TODO: Verify correct
                _ => throw new NotImplementedException(),
            };
        }

        var (key, groupIndex) = DataModel.ParseKeyPart(keys[index]);

        if (currentModel.ValueKind != JsonValueKind.Object || !currentModel.TryGetProperty(key, out JsonElement childModel))
        {
            return null;
        }

        if (childModel.ValueKind == JsonValueKind.Array)
        {
            if (groupIndex is null)
            {
                if (indicies.Length == 0)
                {
                    return null; //Don't know index 
                }

                groupIndex = indicies[0];
            }
            else
            {
                indicies = default; //when you use a literal index, the context indecies are not to be used later.
            }

            var arrayElement = childModel.EnumerateArray().ElementAt((int)groupIndex);
            return GetModelDataRecursive(keys, index + 1, arrayElement, indicies.Length > 0 ? indicies.Slice(1) : indicies);
        }


        return GetModelDataRecursive(keys, index + 1, childModel, indicies);
    }

    /// <inheritdoc />
    public int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default)
    {
        if (_modelRoot is null)
        {
            return null;
        }

        return GetModelDataCountRecurs(key.Split('.'), 0, _modelRoot.Value, indicies);
    }

    private int? GetModelDataCountRecurs(string[] keys, int index, JsonElement currentModel, ReadOnlySpan<int> indicies)
    {
        if (index == keys.Length)
        {
            return null; // Last key part was not an JsonValueKind.Array
        }

        var (key, groupIndex) = DataModel.ParseKeyPart(keys[index]);

        if (currentModel.ValueKind != JsonValueKind.Object || !currentModel.TryGetProperty(key, out JsonElement childModel))
        {
            return null;
        }

        if (childModel.ValueKind == JsonValueKind.Array)
        {
            if (index == keys.Length -1)
            {
                return childModel.GetArrayLength();
            }

            if (groupIndex is null)
            {
                if (indicies.Length == 0)
                {
                    return null; //Don't know index 
                }

                groupIndex = indicies[0];
            }
            else
            {
                indicies = default; //when you use a literal index, the context indecies are not to be used later.
            }

            var arrayElement = childModel.EnumerateArray().ElementAt((int)groupIndex);
            return GetModelDataCountRecurs(keys, index + 1, arrayElement, indicies.Length > 0 ? indicies.Slice(1) : indicies);
        }

        return GetModelDataCountRecurs(keys, index + 1, childModel, indicies);
    }
}


public class DataModel : IDataModelAccessor
{
    private readonly object _serviceModel;

    public DataModel(object serviceModel)
    {
        _serviceModel = serviceModel;
    }


    public object? GetModelData(string key, ReadOnlySpan<int> inidicies)
    {
        return GetModelDataRecursive(key.Split('.'), 0, _serviceModel);
    }


    private object? GetModelDataRecursive(string[] keys, int index, object currentModel)
    {
        if (index == keys.Length)
        {
            return currentModel;
        }

        var key = keys[index];
        // TODO: Support indexed keys (eg; model.repeatingGroup[1].name.value)
        // TODO: Use [JsonPropertyName], [JsonProperty(PropertyName = )], before resorting to actuall property name.
        var prop = currentModel.GetType().GetProperty(
                key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        var childModel = prop?.GetValue(currentModel);
        if (childModel is null)
        {
            return null;
        }

        return GetModelDataRecursive(keys, index + 1, childModel);
    }

    private static Regex KeyPartRegex = new Regex(@"^(\w+)\[(\d+)\]?$");
    public static (string key, int? index) ParseKeyPart(string keypart)
    {
        if (keypart.Last() != ']')
        {
            return (keypart, null);
        }
        var match = KeyPartRegex.Match(keypart);
        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));

    }

    /// <inheritdoc />
    public int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default)
    {
        throw new NotImplementedException();
    }
}