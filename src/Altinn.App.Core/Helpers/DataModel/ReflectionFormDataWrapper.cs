using System.Text.Json;
using Altinn.App.Core.Features;

namespace Altinn.App.Core.Helpers.DataModel;

/// <summary>
/// Get data fields from a model, using string keys (like "Bedrifter[1].Ansatte[1].Alder")
/// </summary>
public class ReflectionFormDataWrapper : IFormDataWrapper
{
    private readonly DataModelWrapper _dataModel;
    private readonly object _rawDataModel;

    /// <summary>
    /// Constructor that wraps a PCOC data model, and gives extra tool for working with the data in an object using json like keys and reflection
    /// </summary>
    public ReflectionFormDataWrapper(object dataModel)
    {
        _dataModel = new DataModelWrapper(dataModel);
        _rawDataModel = dataModel;
    }

    /// <inheritdoc />
    public Type BackingDataType => _rawDataModel.GetType();

    /// <inheritdoc />
    public T BackingData<T>()
        where T : class
    {
        return (T)_rawDataModel;
    }

    /// <inheritdoc />
    public object? GetRaw(ReadOnlySpan<char> path)
    {
        if (path.IsEmpty)
        {
            return null;
        }
        return _dataModel.GetModelData(path.ToString());
    }

    /// <inheritdoc />
    public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption)
    {
        _dataModel.RemoveField(path.ToString(), rowRemovalOption);
    }

    /// <inheritdoc />
    public bool TryAddIndexToPath(
        ReadOnlySpan<char> path,
        ReadOnlySpan<int> rowIndexes,
        Span<char> buffer,
        out ReadOnlySpan<char> indexedPath
    )
    {
        if (rowIndexes.Length == 0)
        {
            indexedPath = path;
            return true;
        }
        string tmp = _dataModel.AddIndicies(path.ToString(), rowIndexes);
        tmp.AsSpan().CopyTo(buffer);
        indexedPath = buffer.Slice(0, tmp.Length);
        return tmp.Length > 0;
    }

    /// <inheritdoc />
    public IFormDataWrapper Copy()
    {
        return FormDataWrapperFactory.Create(
            JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(_rawDataModel), _rawDataModel.GetType())
                ?? throw new InvalidOperationException("Failed to copy data model")
        );
    }

    /// <inheritdoc />
    public void RemoveAltinnRowIds()
    {
        ObjectUtils.RemoveAltinnRowId(_rawDataModel);
    }

    /// <inheritdoc />
    public void InitializeAltinnRowIds()
    {
        ObjectUtils.InitializeAltinnRowId(_rawDataModel);
    }

    /// <inheritdoc />
    public void PrepareModelForXmlStorage()
    {
        ObjectUtils.PrepareModelForXmlStorage(_rawDataModel);
    }
}
