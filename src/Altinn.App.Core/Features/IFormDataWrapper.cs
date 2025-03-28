using System.Collections;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Models.Layout;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface for a wrapper around a data model, that allows for easy access to fields and rows in the model.
///
/// Implementations for each data model type will be created by a source generator and retrieved by the
/// <see cref="FormDataWrapperFactory"/>
/// </summary>
public interface IFormDataWrapper
{
    /// <summary>
    /// Get the C# class type of this form data
    /// </summary>
    public Type BackingDataType { get; }

    /// <summary>
    /// Get the backing data model as a T.
    /// Use &lt;object&gt; if you don't know the type.
    /// </summary>
    /// <code>
    ///     // Get the raw data model as (object)
    ///     var rawData = formDataWrapper.BackingData&lt;object&gt;();
    /// </code>
    /// <exception cref="InvalidCastException">If the wrapped object is not a subtype of T</exception>
    public T BackingData<T>()
        where T : class;

    /// <summary>
    /// Access the raw data model as an object
    /// </summary>
    /// <param name="path">The dotted path to use (including inline indexes)</param>
    object? GetRaw(ReadOnlySpan<char> path);

    // void Set(string path, object? value);
    // void Add(string path, object? value);

    /// <summary>
    /// Remove the field at the given path
    /// If the field points to a row (ends in "[]"), the row will be removed or set to null based on rowRemovalOptions
    /// </summary>
    void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption);

    /// <summary>
    /// Typically you'd never call this low-level method, but rather use one of the extension methods
    /// </summary>
    /// <remarks>
    /// To support relative references, we need to be able to add the indexes from the relative target to the path
    /// </remarks>
    /// <param name="path">The current path (possibly with unset indexes on some collections)</param>
    /// <param name="rowIndexes">Extra rowIndexes that should be added (from context)</param>
    /// <param name="buffer">
    ///     A buffer that the method can work with, that is large enough to hold the full indexed path
    ///     (Typically we use path.Length + rowIndexes.Length * 12, as the indexes might be 10 characters long + "[]")
    /// </param>
    /// <param name="indexedPath">Reference to the part of the buffer that contains the indexed path</param>
    /// <returns>Whether a valid path could be constructed</returns>
    bool TryAddIndexToPath(
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes,
        Span<char> buffer,
        out ReadOnlySpan<char> indexedPath
    );

    /// <summary>
    /// Make a deep copy of the form data
    /// </summary>
    IFormDataWrapper Copy();

    /// <summary>
    /// Set all Guid AltinRowId fields to Guid.Empty (so that they don't get serialized to xml or json)
    /// </summary>
    void RemoveAltinnRowIds();

    /// <summary>
    /// Set all Guid AltinRowId fields that are Guid.Empty to Guid.NewGuid (so that we have an addressable id for the row when diffing for patches)
    /// </summary>
    void InitializeAltinnRowIds();

    /// <summary>
    /// Xml serialization-deserialization does not preserve all properties, and we sometimes need
    /// to know how it looks when it comes back from storage.
    /// </summary>
    /// <remarks>
    /// * Recursively initialize all <see cref="List{T}"/> properties on the object that are currently null
    /// * Ensure that all string properties with `[XmlTextAttribute]` that are empty or whitespace are set to null
    /// * If a class has `[XmlTextAttribute]` and no value, set the parent property to null (if the other properties has [BindNever] attribute)
    /// * If a property has a `ShouldSerialize{PropertyName}` method that returns false, set the property to default value
    /// </remarks>
    void PrepareModelForXmlStorage();
}

/// <summary>
/// Extension methods for <see cref="IFormDataWrapper"/>
/// </summary>
public static class FormDataWrapperExtensions
{
    public static T? Get<T>(
        this IFormDataWrapper formDataWrapper,
        ReadOnlySpan<char> path = default,
        ReadOnlySpan<int> rowIndexes = default
    )
    {
        object? data = formDataWrapper.Get(path, rowIndexes);
        return data switch
        {
            null => default,
            T t => t,
            _ => throw new ArgumentException(
                $"Path {path} does not point to a {typeof(T).FullName}, but {data.GetType().FullName}"
            ),
        };
    }

    public static object? Get(
        this IFormDataWrapper formDataWrapper,
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes = default
    )
    {
        Span<char> buffer = stackalloc char[GetMaxBufferLength(path, rowIndexes)];
        if (formDataWrapper.TryAddIndexToPath(path, rowIndexes, buffer, out var indexedPath))
        {
            return formDataWrapper.GetRaw(indexedPath);
        }

        return null;
    }

    public static string? AddIndexToPath(
        this IFormDataWrapper formDataWrapper,
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes
    )
    {
        Span<char> buffer = stackalloc char[GetMaxBufferLength(path, rowIndexes)];
        if (formDataWrapper.TryAddIndexToPath(path, rowIndexes, buffer, out var indexedPath))
        {
            return indexedPath.ToString();
        }
        return null;
    }

    public static int? GetRowCount(
        this IFormDataWrapper formDataWrapper,
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes
    )
    {
        var data = formDataWrapper.Get(path, rowIndexes);
        return data switch
        {
            null => null,
            ICollection collection => collection.Count,
            _ => throw new ArgumentException(
                $"Path {path} does not point to a collection, but {data.GetType().FullName}"
            ),
        };
    }

    public static DataReference[] GetResolvedKeys(this IFormDataWrapper formDataWrapper, DataReference reference)
    {
        //TODO: write more efficient code that uses the formDataWrapper to resolve keys instead of reflection in DataModelWrapper
        var data = formDataWrapper.BackingData<object>();
        var dataModelWrapper = new DataModelWrapper(data);
        return dataModelWrapper
            .GetResolvedKeys(reference.Field)
            .Select(resolvedField => new DataReference()
            {
                DataElementIdentifier = reference.DataElementIdentifier,
                Field = resolvedField,
            })
            .ToArray();
    }

    private static int GetMaxBufferLength(ReadOnlySpan<char> path, ReadOnlySpan<int> rowIndexes)
    {
        // assume adding indexes adds at most 10 characters per index + "[]"
        // This is way more than we likely need, but int.MaxValue has 10 digits,
        // so there is no reason to accept less.
        const int maxIntStringLength = 10 + 2;
        return path.Length + rowIndexes.Length * maxIntStringLength;
    }
}

/// <summary>
/// Marker interface for <see cref="FormDataWrapperFactory.Create"/> to find the correct implmentation for assembly scanning
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFormDataWrapper<T> : IFormDataWrapper
    where T : class, new() { }
