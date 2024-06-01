using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Utilities for working with model instances
/// </summary>
public static class ObjectUtils
{
    /// <summary>
    /// Set empty Guid properties named "AltinnRowId" to a new random guid
    /// </summary>
    /// <param name="model">The object to mutate</param>
    /// <param name="depth">Remaining recursion depth. To prevent infinite recursion we stop prepeation after this depth. (default matches json serialization)</param>
    public static void InitializeAltinnRowId(object model, int depth = 64)
    {
        ArgumentNullException.ThrowIfNull(model);
        var type = model.GetType();
        if (depth < 0)
        {
            throw new Exception(
                $"Recursion depth exceeded. {type.Name} in {type.Namespace} likely causes infinite recursion."
            );
        }

        if (type.Namespace?.StartsWith("System") == true)
        {
            // System.DateTime.Now causes infinite recursion, and we shuldn't recurse into system types anyway.
            return;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (PropertyIsAltinRowGuid(prop))
            {
                var value = (Guid)(prop.GetValue(model) ?? Guid.Empty);
                if (value == Guid.Empty)
                {
                    // Initialize empty Guid with new random value
                    prop.SetValue(model, Guid.NewGuid());
                }
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var value = prop.GetValue(model);
                if (value is not null)
                {
                    foreach (var item in (IList)value)
                    {
                        // Recurse into values of a list
                        if (item is not null)
                        {
                            InitializeAltinnRowId(item, depth - 1);
                        }
                    }
                }
            }
            // property does not have an index parameter, thus we should recurse into the property
            else if (prop.GetIndexParameters().Length == 0)
            {
                var value = prop.GetValue(model);

                // continue recursion over all properties that are not null or value types
                if (value is not null)
                {
                    InitializeAltinnRowId(value, depth - 1);
                }
            }
        }
    }

    /// <summary>
    /// Xml serialization-deserialization does not preserve all properties, and we sometimes need
    /// to know how it looks when it comes back from storage.
    /// * Recursively initialize all <see cref="List{T}"/> properties on the object that are currently null
    /// * Ensure that all string properties with `[XmlTextAttribute]` that are empty or whitespace are set to null
    /// * If a class has `[XmlTextAttribute]` and no value, set the parent property to null (if the other properties has [BindNever] attribute)
    /// * If a class has a method `bool ShouldSerialize{PropertyName}()`, and it returns false, set the property to null
    /// * Round all decimal and long to 15 significant digits to avoid precision loss in js frontend
    /// </summary>
    /// <param name="model">The object to mutate</param>
    /// <param name="depth">Remaining recursion depth. To prevent infinite recursion we stop prepeation after this depth. (default matches json serialization)</param>
    public static void PrepareModelForXmlStorage(object model, int depth = 64)
    {
        ArgumentNullException.ThrowIfNull(model);
        var type = model.GetType();
        if (depth < 0)
        {
            throw new Exception(
                $"Recursion depth exceeded. {type.Name} in {type.Namespace} likely causes infinite recursion."
            );
        }

        if (type.Namespace?.StartsWith("System") == true)
        {
            // System.DateTime.Now causes infinite recursion, and we shuldn't recurse into system types anyway.
            return;
        }

        var methodInfos = type.GetMethods();

        // Iterate over properties of the model
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // Handle properties of type List
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var value = prop.GetValue(model);
                if (value is null)
                {
                    // Initialize List if it has null value (xml deserialization always retrurn emtpy list, not null)
                    prop.SetValue(model, Activator.CreateInstance(prop.PropertyType));
                }
                else
                {
                    // Recurse into values of a list
                    foreach (var item in (IList)value)
                    {
                        if (item is not null)
                        {
                            PrepareModelForXmlStorage(item, depth - 1);
                        }
                    }
                }
            }
            else if (prop.GetIndexParameters().Length == 0)
            {
                // Property does not have an index parameter, thus we should recurse into the property
                var value = prop.GetValue(model);

                // Modify values that will get modified in serialization anyways
                switch (value)
                {
                    case decimal decimalValue:
                        // Frontend will parse json numbers as 64 bit floating point numbers
                        // This will cause precision loss for numbers with more than 15 significant digits,
                        // causing issues in PATCH requests where FE don't report the same value as backend stores.
                        //
                        // To ensure that we don't pretend to have more precision than we actually have,
                        // we will round all decimals to 15 significant digits.
                        // PS: Directly rounding would likely be more efficient, but there was no convenient method for rounding
                        // decimals to a specific number of significant digits.

                        decimal roundedTo15Decimals = (decimal)(double)decimalValue; // This does rounding to 15 significant figures
                        if (
                            roundedTo15Decimals != decimalValue
                            && prop.GetCustomAttribute<JsonIgnoreAttribute>()
                                is null
                                    or { Condition: not JsonIgnoreCondition.Always }
                        )
                        {
                            // TODO: consider logging a warning if rounding is done
                            //       It should be explicit in backend code.
                            prop.SetValue(model, roundedTo15Decimals);
                        }
                        break;
                    case long longValue when longValue is > 999_999_999_999_999 or < -999_999_999_999_999:
                        // Same as with decimals, we need to round longs to 15 digits to avoid precision loss in frontend
                        long roundedTo15Digits = (long)(decimal)(double)longValue;
                        if (
                            roundedTo15Digits != longValue
                            && prop.GetCustomAttribute<JsonIgnoreAttribute>()
                                is null
                                    or { Condition: not JsonIgnoreCondition.Always }
                        )
                        {
                            prop.SetValue(model, roundedTo15Digits);
                        }
                        break;
                    case string stringValue:
                        if (
                            string.IsNullOrWhiteSpace(stringValue)
                            && prop.GetCustomAttribute<XmlTextAttribute>() is not null
                        )
                        {
                            // Set string properties with [XmlText] attribute to null if they are empty or whitespace
                            // because xml serialzation does this
                            prop.SetValue(model, null);
                            value = null;
                        }
                        break;
                }

                if (value is not null)
                {
                    // continue recursion over all properties that are NOT null or value types
                    PrepareModelForXmlStorage(value, depth - 1);

                    if (ShouldSerializeReturnsFalse(model, prop, methodInfos) && prop.SetMethod is not null)
                    {
                        prop.SetValue(model, null);
                    }
                }
            }
        }
    }

    private static bool ShouldSerializeReturnsFalse(object model, PropertyInfo prop, MethodInfo[] methodInfos)
    {
        string methodName = $"ShouldSerialize{prop.Name}";

        var shouldSerializeMethod = methodInfos
            .Where(m => m.Name == methodName && m.GetParameters().Length == 0 && m.ReturnType == typeof(bool))
            .SingleElement();

        return shouldSerializeMethod?.Invoke(model, null) is false;
    }

    private static T? SingleElement<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return default;
        }
        var result = enumerator.Current;
        return enumerator.MoveNext() ? default : result;
    }

    /// <summary>
    /// Set all <see cref="Guid"/> properties named "AltinnRowId" to Guid.Empty
    /// </summary>
    public static void RemoveAltinnRowId(object model, int depth = 64)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (depth < 0)
        {
            throw new Exception(
                $"Recursion depth exceeded. {model.GetType().Name} in {model.GetType().Namespace} likely causes infinite recursion."
            );
        }
        var type = model.GetType();
        if (type.Namespace?.StartsWith("System") == true)
        {
            // System.DateTime.Now causes infinite recursion, and we shuldn't recurse into system types anyway.
            return;
        }

        foreach (var prop in type.GetProperties())
        {
            // Handle guid fields named "AltinnRowId"
            if (PropertyIsAltinRowGuid(prop))
            {
                prop.SetValue(model, Guid.Empty);
            }
            // Recurse into lists
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var value = prop.GetValue(model);
                if (value is not null)
                {
                    foreach (var item in (IList)value)
                    {
                        // Recurse into values of a list
                        if (item is not null)
                        {
                            RemoveAltinnRowId(item, depth - 1);
                        }
                    }
                }
            }
            // Recurse into all properties that are not lists
            else if (prop.GetIndexParameters().Length == 0)
            {
                var value = prop.GetValue(model);

                // continue recursion over all properties
                if (value is not null)
                {
                    RemoveAltinnRowId(value, depth - 1);
                }
            }
        }
    }

    private static bool PropertyIsAltinRowGuid(PropertyInfo prop)
    {
        return prop.PropertyType == typeof(Guid) && prop.Name == "AltinnRowId";
    }
}
