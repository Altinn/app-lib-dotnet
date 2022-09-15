using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Implementation.Expression;

/// <summary>
/// Interface for accessing fields in the data model
/// </summary>
public interface IDataModelAccessor
{
    /// <summary>
    /// Get model data based on key and optionally indicies
    /// </summary>
    /// <remarks>
    /// Inline indicies in the key "Bedrifter[1].Ansatte[1].Alder" will override
    /// normal indicies, and if both "Bedrifter" and "Ansatte" is lists,
    /// "Bedrifter[1].Ansatte.Alder", will fail, because the indicies will be reset
    /// after an inline index is used
    /// </remarks>
    object? GetModelData(string key, ReadOnlySpan<int> indicies = default);

    /// <summary>
    /// Get the count of data elements set in a group (enumerable)
    /// </summary>
    int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default);

    string? AddIndicies(string key, ReadOnlySpan<int> indicies = default);
}

/// <summary>
/// Implementation of <see cref="IDataModelAccessor" /> for data models based on JsonElement (mainliy for testing )
/// </summary>
/// <remarks>
/// This class is written to enable the use of shared tests (with frontend) where the datamodel is defined
/// in json. It's hard to IL generate proper C# classes to use the normal <see cref="DataModel" /> in tests
/// </remarks>
public class JsonDataModel : IDataModelAccessor
{
    private readonly JsonElement? _modelRoot;

    /// <summary>
    /// Constructor that creates a JsonDataModel based on a JsonElement
    /// </summary>
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
            if (index == keys.Length - 1)
            {
                return childModel.GetArrayLength();
            }

            if (groupIndex is null)
            {
                if (indicies.Length == 0)
                {
                    return null; // Error index for collection not specified
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

    /// <inheritdoc />
    public string? AddIndicies(string key, ReadOnlySpan<int> indicies)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Get data fields from a model, using string keys (like "Bedrifter[1].Ansatte[1].Alder")
/// </summary>
public class DataModel : IDataModelAccessor
{
    private readonly object _serviceModel;

    public DataModel(object serviceModel)
    {
        _serviceModel = serviceModel;
    }


    public object? GetModelData(string key, ReadOnlySpan<int> indicies)
    {
        return GetModelDataRecursive(key.Split('.'), 0, _serviceModel, indicies);
    }

    /// <inheritdoc />
    public int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default)
    {
        return GetModelDataRecursive(key.Split('.'), 0, _serviceModel, indicies) as int?;
    }

    private object? GetModelDataRecursive(string[] keys, int index, object currentModel, ReadOnlySpan<int> indicies)
    {
        if (index == keys.Length)
        {
            return currentModel;
        }

        var (key, groupIndex) = ParseKeyPart(keys[index]);
        var prop = currentModel.GetType().GetProperties().FirstOrDefault(p => IsPropertyWithJsonName(p, key));
        if (prop is null)
        {
            //throw new Exception($"Unknown model property {key} in {string.Join('.', keys)}");
            return null;
        }

        var childModel = prop.GetValue(currentModel);
        if (childModel is null)
        {
            return null;
        }

        // Strings are enumerable in C#
        // Other enumerable types is treated as an collection
        if (childModel is not string && childModel is System.Collections.IEnumerable childModelList)
        {
            if (index == keys.Length - 1)
            {
                // The Linq IEnumerable<T>.Count() does not work on non generic enumerable
                // that we need to use with reflection
                int childCount = 0;
                foreach (var _ in childModelList)
                {
                    childCount++;
                }
                return childCount;
            }
            if (groupIndex is null)
            {
                if (indicies.Length == 0)
                {
                    return null; // Error index for collection not specified
                }

                groupIndex = indicies[0];
            }
            else
            {
                indicies = default; //when you use a literal index, the context indecies are not to be used later.
            }

            foreach (var arrayElement in childModelList)
            {
                if (groupIndex-- < 1)
                {
                    return GetModelDataRecursive(keys, index + 1, arrayElement, indicies.Length > 0 ? indicies.Slice(1) : indicies);
                }
            }
        }

        return GetModelDataRecursive(keys, index + 1, childModel, indicies);
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

    private static void AddIndiciesRecursive(List<string> ret, Type currentModelType, ReadOnlySpan<string> keys, string fullKey, ReadOnlySpan<int> indicies)
    {
        if (keys.Length == 0)
        {
            return;
        }
        var (key, groupIndex) = ParseKeyPart(keys[0]);
        var prop = currentModelType.GetProperties().FirstOrDefault(p => IsPropertyWithJsonName(p, key));
        if (prop is null)
        {
            throw new Exception($"Unknown model property {key} in {fullKey}");
        }

        var type = prop.PropertyType;
        if (type != typeof(string) && type.IsAssignableTo(typeof(System.Collections.IEnumerable)))
        {

            if (groupIndex is null)
            {
                ret.Add($"{key}[{indicies[0]}]");
            }
            else
            {
                ret.Add($"{key}[{groupIndex}]");
                indicies = default;
            }

            AddIndiciesRecursive(ret, type, keys.Slice(1), fullKey, indicies.Slice(1));
            return;
        }

        if (groupIndex is not null)
        {
            throw new Exception("Index on non indexable property");
        }


    }

    /// <inheritdoc />
    public string? AddIndicies(string key, ReadOnlySpan<int> indicies)
    {
        if (indicies.Length == 0)
        {
            return key;
        }

        var ret = new List<string>();
        AddIndiciesRecursive(ret, this._serviceModel.GetType(), key.Split('.'), key, indicies);
        return string.Join('.', ret);
    }

    private static bool IsPropertyWithJsonName(PropertyInfo propertyInfo, string key)
    {
        var ca = propertyInfo.CustomAttributes;
        var system_text_json_attribute = (ca.FirstOrDefault(attr => attr.AttributeType.FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute")?.ConstructorArguments.FirstOrDefault().Value as string);
        if (system_text_json_attribute is not null)
        {
            return system_text_json_attribute == key;
        }

        var newtonsoft_json_attribute = (ca.FirstOrDefault(attr => attr.AttributeType.FullName == "Newtonsoft.Json.JsonPropertyAttribute")?.ConstructorArguments.FirstOrDefault().Value as string);
        if (newtonsoft_json_attribute is not null)
        {
            return newtonsoft_json_attribute == key;
        }

        // Fallback to property name if all attributes could not be found
        var keyName = propertyInfo.Name;
        return keyName == key;
    }
}