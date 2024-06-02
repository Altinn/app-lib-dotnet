using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers;

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
    object? GetModelData(ModelBinding key, ReadOnlySpan<int> indicies = default);

    /// <summary>
    /// Get the count of data elements set in a group (enumerable)
    /// </summary>
    int? GetModelDataCount(ModelBinding key, ReadOnlySpan<int> indicies = default);

    /// <summary>
    /// Get all of the resolved keys (including all possible indexes) from a data model key
    /// </summary>
    ModelBinding[] GetResolvedKeys(ModelBinding key);

    /// <summary>
    /// Return a full dataModelBiding from a context aware binding by adding indicies
    /// </summary>
    /// <example>
    /// key = "bedrift.ansatte.navn"
    /// indicies = [1,2]
    /// => "bedrift[1].ansatte[2].navn"
    /// </example>
    ModelBinding AddIndicies(ModelBinding key, ReadOnlySpan<int> indicies = default);

    /// <summary>
    /// Remove a value from the wrapped datamodel
    /// </summary>
    void RemoveField(ModelBinding key, RowRemovalOption rowRemovalOption);

    /// <summary>
    /// Verify that a Key is a valid lookup for the datamodel
    /// </summary>
    bool VerifyKey(ModelBinding key);

    /// <summary>
    /// Get the data element that is used when ModelBinding don't have a <see cref="ModelBinding.Field"/>>
    /// </summary>
    DataElement DefaultDataElement { get; }
}

/// <summary>
/// Option for how to handle row removal
/// </summary>
public enum RowRemovalOption
{
    /// <summary>
    /// Remove the row from the data model
    /// </summary>
    DeleteRow,

    /// <summary>
    /// Set the row to null, used to preserve row indices
    /// </summary>
    SetToNull,

    /// <summary>
    /// Ignore row removal
    /// </summary>
    Ignore
}
