using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    public static void InitializeAltinnRowId(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        foreach (var prop in model.GetType().GetProperties())
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
                            InitializeAltinnRowId(item);
                        }
                    }
                }
            }
            // property does not have an index parameter, nor is a value type, thus we should recurse into the property
            else if (prop.GetIndexParameters().Length == 0 && prop.PropertyType.IsValueType == false)
            {
                var value = prop.GetValue(model);

                // continue recursion over all properties that are not null or value types
                if (value is not null)
                {
                    InitializeAltinnRowId(value);
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
    /// </summary>
    /// <param name="model">The object to mutate</param>
    public static void PrepareModelForXmlStorage(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Iterate over properties of the model
        foreach (var prop in model.GetType().GetProperties())
        {
            // Property has a generic type that is a subtype of List<>
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var value = prop.GetValue(model);
                if (value is null)
                {
                    // Initialize IList with null value (this is what comes back from xml deserialization)
                    prop.SetValue(model, Activator.CreateInstance(prop.PropertyType));
                }
                else
                {
                    // Recurse into values of a list
                    foreach (var item in (IList)value)
                    {
                        if (item is not null)
                        {
                            PrepareModelForXmlStorage(item);
                        }
                    }
                }
            }
            else if (prop.GetIndexParameters().Length > 0)
            {
                // Ignore properties with index parameters
            }
            else
            {
                // Property does not have an index parameter, thus we should recurse into the property
                var value = prop.GetValue(model);
                if (value is null)
                    continue;

                // Only touch string properties with the [XmlText] attribute and contains only whitespace
                if (value is string s &&
                string.IsNullOrWhiteSpace(s) &&
                prop.GetCustomAttributes<XmlTextAttribute>().SingleElement() is not null)
                {
                    // Ensure empty strings are set to null
                    prop.SetValue(model, null);
                }

                // continue recursion over all properties that are NOT null or value types
                else if (value.GetType().IsValueType == false)
                {
                    PrepareModelForXmlStorage(value);
                }

                NullParentIfEmptyXmlTextProperty(model, value, prop);
            }
        }
    }

    private static void NullParentIfEmptyXmlTextProperty(object model, object value, PropertyInfo propertyInfo)
    {
        var properties = value.GetType().GetProperties();
        // Get the number of properties that have the [BindNever] attribute (typically fixed values)
        var attributePropertyCount = properties.Count(p => p.GetCustomAttributes<BindNeverAttribute>().Any());
        if (attributePropertyCount + 1 != properties.Length)
        {
            // If there are more than one property WITHOUT the [BindNever] attribute, we should not null the parent
            return;
        }

        // Get the property that has the [XmlText] attribute
        var xmlTextProperty = properties.Where(p => p.GetCustomAttributes<XmlTextAttribute>().Any()).SingleElement();

        if (xmlTextProperty is null)
        {
            return;
        }

        if (xmlTextProperty.GetValue(value) is null)
        {
            // set the parent property to null if the [XmlText] property is null
            propertyInfo.SetValue(model, null);
        }
    }

    private static T? SingleElement<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return default(T);
        }
        var result = enumerator.Current;
        return enumerator.MoveNext() ? default(T) : result;
    }

    /// <summary>
    /// Set all <see cref="Guid"/> properties named "AltinnRowId" to Guid.Empty
    /// </summary>
    public static void RemoveAltinnRowId(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        foreach (var prop in model.GetType().GetProperties())
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
                            RemoveAltinnRowId(item);
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
                    RemoveAltinnRowId(value);
                }
            }
        }
    }

    private static bool PropertyIsAltinRowGuid(PropertyInfo prop)
    {
        return prop.PropertyType == typeof(Guid) && prop.Name == "AltinnRowId";
    }
}