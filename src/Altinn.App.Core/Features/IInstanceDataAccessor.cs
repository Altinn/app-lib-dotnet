using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
/// Service for accessing data from other data elements in the
/// </summary>
public interface IInstanceDataAccessor
{
    /// <summary>
    /// The instance that the accessor can access data for.
    /// </summary>
    Instance Instance { get; }

    /// <summary>
    /// Get the data types from application metadata.
    /// </summary>
    IReadOnlyCollection<DataType> DataTypes { get; }

    /// <summary>
    /// Get the actual data represented in the data element.
    /// </summary>
    /// <returns>The deserialized data model for this data element</returns>
    /// <exception cref="InvalidOperationException">when identifier does not exist in instance.Data with an applogic data type</exception>
    Task<object> GetFormData(DataElementIdentifier dataElementIdentifier);

    /// <summary>
    /// Get the actual data represented in the data element wrapped in an <see cref="IFormDataWrapper"/>.
    /// </summary>
    /// <returns>The deserialized data model for this data element</returns>
    /// <exception cref="InvalidOperationException">when identifier does not exist in instance.Data with an applogic data type</exception>
    Task<IFormDataWrapper> GetFormDataWrapper(DataElementIdentifier dataElementIdentifier);

    /// <summary>
    /// Get a <see cref="IInstanceDataAccessor"/> that provides access to the cleaned data (where all fields marked as "hidden" is removed).
    /// </summary>
    /// <param name="rowRemovalOption">The strategy for "hiddenRow" on group components</param>
    IInstanceDataAccessor GetCleanAccessor(RowRemovalOption rowRemovalOption = RowRemovalOption.SetToNull);

    /// <summary>
    /// Gets the raw binary data from a DataElement.
    /// </summary>
    /// <remarks>Form data elements (with appLogic) will get json serialized UTF-8</remarks>
    /// <exception cref="InvalidOperationException">when identifier does not exist in instance.Data</exception>
    Task<ReadOnlyMemory<byte>> GetBinaryData(DataElementIdentifier dataElementIdentifier);

    /// <summary>
    /// Get a data element from an instance by id,
    /// </summary>
    /// <exception cref="InvalidOperationException">If the data element is not found on the instance</exception>
    DataElement GetDataElement(DataElementIdentifier dataElementIdentifier);
}

/// <summary>
/// Extension methods for IInstanceDataAccessor to simplify usage.
/// </summary>
public static class IInstanceDataAccessorExtensions
{
    /// <summary>
    /// Get the data type from application with the given type.
    /// </summary>
    /// <returns>The data type (or null if it does not exist)</returns>
    public static DataType? GetDataType(this IInstanceDataAccessor dataAccessor, string dataTypeId)
    {
        return dataAccessor.DataTypes.FirstOrDefault(dataType =>
            dataTypeId.Equals(dataType.Id, StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Get the dataType of a data element.
    /// </summary>
    /// <throws>Throws an InvalidOperationException if the data element is not found on the instance</throws>
    public static DataType GetDataType(
        this IInstanceDataAccessor dataAccessor,
        DataElementIdentifier dataElementIdentifier
    )
    {
        var dataElement = dataAccessor.GetDataElement(dataElementIdentifier);
        var dataType = dataAccessor.GetDataType(dataElement.DataType);
        if (dataType is null)
        {
            throw new InvalidOperationException(
                $"Data type {dataElement.DataType} not found in applicationmetadata.json"
            );
        }

        return dataType;
    }

    /// <summary>
    /// Get the actual data represented in the data element (cast into T).
    /// </summary>
    /// <returns>The deserialized data model for this data element or an exception for non-form data elements</returns>
    public static async Task<T> GetFormData<T>(
        this IInstanceDataAccessor accessor,
        DataElementIdentifier dataElementIdentifier
    )
        where T : class
    {
        IFormDataWrapper data = await accessor.GetFormDataWrapper(dataElementIdentifier);
        return data.BackingData<T>();
    }

    /// <summary>
    /// Extension method to get formdata from a type parameter that must match dataType.AppLogic.ClassRef.
    /// </summary>
    /// <remarks>
    /// This method only supports data types with MaxCount = 1.
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the form data element can't be found, or if it can support more than one data element</exception>
    public static async Task<T> GetFormData<T>(this IInstanceDataAccessor accessor)
        where T : class
    {
        var dataType = accessor.GetDataType<T>();
        return await accessor.GetFormData<T>(dataType);
    }

    /// <summary>
    /// Get an array of all the form data elements that has the given type as AppLogic.ClassRef.
    /// </summary>
    public static async Task<T[]> GetAllFormData<T>(this IInstanceDataAccessor accessor)
        where T : class
    {
        var dataType = accessor.GetDataType<T>();
        return await accessor.GetAllFormData<T>(dataType);
    }

    /// <summary>
    /// Get an array of all the form data elements that has the given DataType
    /// </summary>
    public static async Task<T[]> GetAllFormData<T>(this IInstanceDataAccessor accessor, DataType dataType)
        where T : class
    {
        var dataElements = accessor.GetDataElementsForType(dataType).ToArray();
        var result = new T[dataElements.Length];
        for (int i = 0; i < dataElements.Length; i++)
        {
            result[i] = await accessor.GetFormData<T>(dataElements[i]);
        }

        return result;
    }

    /// <summary>
    /// Get form data from a specific data type. (The data type must have MaxCount = 1)
    /// </summary>
    public static async Task<T> GetFormData<T>(this IInstanceDataAccessor accessor, DataType dataType)
        where T : class
    {
        if (dataType.MaxCount != 1)
        {
            throw new InvalidOperationException(
                $"Data type {dataType.Id} is not a single instance data type, but has MaxCount = {dataType.MaxCount}"
            );
        }

        var dataTypeId = dataType.Id;

        var dataElement = accessor.Instance.Data.FirstOrDefault(dataElement =>
            dataTypeId.Equals(dataElement.DataType, StringComparison.Ordinal)
        );
        if (dataElement is null)
        {
            throw new InvalidOperationException($"Data element for data type {dataTypeId} not found in instance");
        }

        return await accessor.GetFormData<T>(dataElement);
    }

    /// <summary>
    /// Get the data type from a C# class reference.
    ///
    /// Note that this throws an error if multiple data types have a ClassRef that matches the given type.
    /// </summary>
    /// <exception cref="InvalidOperationException">If multiple dataType have a ClassRef that matches the given type</exception>
    public static DataType GetDataType<T>(this IInstanceDataAccessor accessor)
    {
        var dataTypes = accessor.DataTypes.Where(d => d.AppLogic?.ClassRef == typeof(T).FullName).ToArray();
        if (dataTypes.Length == 1)
        {
            return dataTypes[0];
        }

        if (dataTypes.Length == 0)
        {
            throw new InvalidOperationException(
                $"Data type for {typeof(T).FullName} not found in applicationmetadata.json"
            );
        }

        throw new InvalidOperationException(
            $"Multiple data types found that references {typeof(T).FullName} found for multiple in applicationmetadata.json ({string.Join(", ", dataTypes.Select(d => d.Id))}). This means you can't access data just based on type parameter<T>."
        );
    }

    /// <summary>
    /// Get all elements of a specific data type.
    /// </summary>
    public static IEnumerable<DataElement> GetDataElementsForType(
        this IInstanceDataAccessor accessor,
        string dataTypeId
    ) => accessor.Instance.Data.Where(dataElement => dataTypeId.Equals(dataElement.DataType, StringComparison.Ordinal));

    /// <summary>
    /// Get all elements of a specific data type.
    /// </summary>
    public static IEnumerable<DataElement> GetDataElementsForType(
        this IInstanceDataAccessor accessor,
        DataType dataType
    ) => accessor.GetDataElementsForType(dataType.Id);

    /// <summary>
    /// Retrieves the data elements associated with a specific task.
    /// </summary>
    /// <returns>An enumerable collection of tuples containing the data type and data element associated with the specified task.</returns>
    public static IEnumerable<(DataType dataType, DataElement dataElement)> GetDataElementsForTask(
        this IInstanceDataAccessor accessor,
        string taskId
    )
    {
        foreach (var dataElement in accessor.Instance.Data)
        {
            var dataType = accessor.GetDataType(dataElement.DataType);
            if (taskId.Equals(dataType?.TaskId, StringComparison.Ordinal))
            {
                yield return (dataType, dataElement);
            }
        }
    }

    /// <summary>
    /// Get all form data elements along with their data types.
    /// </summary>
    public static IEnumerable<(DataType dataType, DataElement dataElement)> GetDataElementsWithFormData(
        this IInstanceDataAccessor accessor
    )
    {
        foreach (var dataElement in accessor.Instance.Data)
        {
            var dataType = accessor.GetDataType(dataElement.DataType);
            if (dataType?.AppLogic?.ClassRef is not null)
            {
                yield return (dataType, dataElement);
            }
        }
    }

    /// <summary>
    /// Get all form data elements along with their data types for a specific task.
    /// </summary>
    public static IEnumerable<(DataType dataType, DataElement dataElement)> GetDataElementsWithFormDataForTask(
        this IInstanceDataAccessor accessor,
        string taskId
    )
    {
        foreach (var dataElement in accessor.Instance.Data)
        {
            var dataType = accessor.GetDataType(dataElement.DataType);
            if (dataType?.AppLogic?.ClassRef is not null && taskId.Equals(dataType.TaskId, StringComparison.Ordinal))
            {
                yield return (dataType, dataElement);
            }
        }
    }

    /// <summary>
    /// Get all data elements along with their data types.
    /// </summary>
    public static IEnumerable<(DataType dataType, DataElement dataElement)> GetDataElements(
        this IInstanceDataAccessor accessor
    )
    {
        foreach (var dataElement in accessor.Instance.Data)
        {
            var dataType = accessor.GetDataType(dataElement.DataType);
            if (dataType is not null)
            {
                yield return (dataType, dataElement);
            }
        }
    }
}
