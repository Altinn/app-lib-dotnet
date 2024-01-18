using System.Collections;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Utilities for working with model instances
/// </summary>
public static class ObjectUtils
{
    /// <summary>
    /// Recursively initialize all <see cref="List{T}"/> properties on the object that are currently null
    /// </summary>
    /// <param name="model"></param>
    public static void InitializeListsRecursively(object model)
    {
        foreach (var prop in model.GetType().GetProperties())
        {
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var value = prop.GetValue(model);
                if (value is null)
                {
                    // Initialize IList with null value
                    prop.SetValue(model, Activator.CreateInstance(prop.PropertyType));
                }
                else
                {
                    foreach (var item in (IList)value)
                    {
                        // Recurse into values of a list
                        InitializeListsRecursively(item);
                    }
                }
            }
            else if (prop.GetIndexParameters().Length == 0)
            {
                var value = prop.GetValue(model);

                if (value is "")
                {
                    // Initialize string with null value (xml serialization does not always preserve "")
                    prop.SetValue(model, null);
                }

                // continue recursion over all properties
                if (value is not null)
                {
                    InitializeListsRecursively(value);
                }
            }
        }
    }
}