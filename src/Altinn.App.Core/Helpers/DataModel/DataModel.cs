using System.Text.RegularExpressions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers.DataModel;

/// <summary>
/// Get data fields from a model, using string keys (like "Bedrifter[1].Ansatte[1].Alder")
/// </summary>
public class DataModel
{
    private readonly IInstanceDataAccessor _dataAccessor;
    private readonly Dictionary<string, DataElementIdentifier> _dataIdsByType = [];

    /// <summary>
    /// Constructor that wraps a POCO data model, and gives extra tool for working with the data
    /// </summary>
    public DataModel(IInstanceDataAccessor dataAccessor)
    {
        _dataAccessor = dataAccessor;
        foreach (var (dataType, dataElement) in dataAccessor.GetDataElements())
        {
            if (dataType is { MaxCount: 1, AppLogic.ClassRef: not null })
            {
                _dataIdsByType.TryAdd(dataElement.DataType, dataElement);
            }
        }
    }

    /// <summary>
    /// Get access to the instance object
    /// </summary>
    public Instance Instance => _dataAccessor.Instance;

    private async Task<IFormDataWrapper> ServiceModel(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier
    )
    {
        return (await ServiceModelAndDataElementId(key, defaultDataElementIdentifier)).model;
    }

    private async Task<(DataElementIdentifier dataElementId, IFormDataWrapper model)> ServiceModelAndDataElementId(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier
    )
    {
        if (
            key.DataType == null
            || defaultDataElementIdentifier.DataTypeId == key.DataType
            || _dataAccessor.GetDataType(defaultDataElementIdentifier).Id == key.DataType
        )
        {
            return (defaultDataElementIdentifier, await _dataAccessor.GetFormDataWrapper(defaultDataElementIdentifier));
        }

        if (_dataIdsByType.TryGetValue(key.DataType, out var dataElementId))
        {
            return (dataElementId, await _dataAccessor.GetFormDataWrapper(dataElementId));
        }
        if (_dataAccessor.GetDataType(key.DataType) is { } dataType)
        {
            if (dataType.MaxCount != 1)
            {
                throw new InvalidOperationException(
                    $"{key.DataType} has maxCount different from 1 in applicationmetadata.json or don't have a classRef in appLogic"
                );
            }
            return (dataElementId, await _dataAccessor.GetFormDataWrapper(dataElementId));
        }

        throw new InvalidOperationException(
            $"Data model with type {key.DataType} not found in applicationmetadata.json"
        );
    }

    /// <summary>
    /// Get model data based on key and optionally indexes
    /// </summary>
    /// <remarks>
    /// Inline indicies in the key "Bedrifter[1].Ansatte[1].Alder" will override
    /// normal indicies, and if both "Bedrifter" and "Ansatte" is lists,
    /// "Bedrifter[1].Ansatte.Alder", will fail, because the indicies will be reset
    /// after an inline index is used
    /// </remarks>
    public async Task<object?> GetModelData(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes
    )
    {
        var model = await ServiceModel(key, defaultDataElementIdentifier);
        return model.Get(key.Field, rowIndexes);
    }

    /// <summary>
    /// Get the count of data elements set in a group (enumerable)
    /// </summary>
    public async Task<int?> GetModelDataCount(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes
    )
    {
        var model = await ServiceModel(key, defaultDataElementIdentifier);
        return model.GetRowCount(key.Field, rowIndexes);
    }

    /// <summary>
    /// Get an array of all keys in repeating groups that match this key
    /// </summary>
    /// <example>
    /// GetResolvedKeys("data.bedrifter.styre.medlemmer") =>
    /// [
    ///     "data.bedrifter[0].styre.medlemmer",
    ///     "data.bedrifter[1].styre.medlemmer"
    /// ]
    /// </example>
    public async Task<DataReference[]> GetResolvedKeys(DataReference reference)
    {
        var data = await _dataAccessor.GetFormDataWrapper(reference.DataElementIdentifier);
        return data.GetResolvedKeys(reference);
    }

    private static readonly Regex _rowIndexRegex = new(
        @"^([^[\]]+(\[(\d+)])?)+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(10)
    );

    /// <summary>
    /// Get the row indices from a key
    /// </summary>
    public static int[]? GetRowIndices(string field)
    {
        var match = _rowIndexRegex.Match(field);
        var rowIndices = match.Groups[3].Captures.Select(c => c.Value).Select(int.Parse).ToArray();
        return rowIndices.Length == 0 ? null : rowIndices;
    }

    /// <summary>
    /// Return a full dataModelBiding from a context aware binding by adding indicies
    /// </summary>
    /// <example>
    /// key = "bedrift.ansatte.navn"
    /// indicies = [1,2]
    /// => "bedrift[1].ansatte[2].navn"
    /// </example>
    public async Task<DataReference> AddIndexes(
        ModelBinding key,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes
    )
    {
        var (dataElementId, formDataWrapper) = await ServiceModelAndDataElementId(key, defaultDataElementIdentifier);

        var field = formDataWrapper.AddIndexToPath(key.Field, rowIndexes) ?? "";
        return new DataReference() { Field = field, DataElementIdentifier = dataElementId };
    }

    /// <summary>
    /// Set the value of a field in the model to default (null)
    /// </summary>
    public async Task RemoveField(DataReference reference, RowRemovalOption rowRemovalOption)
    {
        var dataWrapper = await _dataAccessor.GetFormDataWrapper(reference.DataElementIdentifier);
        dataWrapper.RemoveField(reference.Field, rowRemovalOption);
    }
}
