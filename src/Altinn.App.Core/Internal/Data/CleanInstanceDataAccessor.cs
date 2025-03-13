using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Data;

internal class CleanInstanceDataAccessor : IInstanceDataAccessor
{
    private readonly IInstanceDataAccessor _dataMutator;
    private readonly RowRemovalOption _rowRemovalOption;

    public CleanInstanceDataAccessor(
        IInstanceDataMutator dataMutator,
        LayoutEvaluatorState state,
        RowRemovalOption rowRemovalOption
    )
    {
        _dataMutator = dataMutator;
        _rowRemovalOption = rowRemovalOption;
        _hiddenFieldsTask = new(() => LayoutEvaluator.GetHiddenFieldsForRemoval(state));
    }

    private readonly DataElementCache<IFormDataWrapper> _cleanCache = new();

    private readonly Lazy<Task<List<DataReference>>> _hiddenFieldsTask;

    public Instance Instance => _dataMutator.Instance;

    public async Task<object> GetFormData(DataElementIdentifier dataElementId)
    {
        return (await GetFormDataWrapper(dataElementId)).BackingData<object>();
    }

    public async Task<IFormDataWrapper> GetFormDataWrapper(DataElementIdentifier dataElementIdentifier)
    {
        return await _cleanCache.GetOrCreate(
            dataElementIdentifier,
            async () =>
            {
                var data = await _dataMutator.GetFormDataWrapper(dataElementIdentifier);
                // Shortcut without copying if there are no hidden fields
                var hiddenFields = await _hiddenFieldsTask.Value;
                if (hiddenFields.All(dr => dr.DataElementIdentifier.Guid != dataElementIdentifier.Guid))
                {
                    return data;
                }

                return CleanModel(data.Copy(), dataElementIdentifier, hiddenFields, _rowRemovalOption);
            }
        );
    }

    private static IFormDataWrapper CleanModel(
        IFormDataWrapper data,
        DataElementIdentifier dataElementIdentifier,
        List<DataReference> hiddenFields,
        RowRemovalOption rowRemovalOption
    )
    {
        foreach (var dataReference in hiddenFields)
        {
            if (dataReference.DataElementIdentifier != dataElementIdentifier)
            {
                continue;
            }

            // Note that the paths for lists is in reverse order from GetHiddenFieldsForRemoval, so we can remove them here in order
            data.RemoveField(dataReference.Field, rowRemovalOption);
        }

        return data;
    }

    public IInstanceDataAccessor GetCleanAccessor(RowRemovalOption rowRemovalOption = RowRemovalOption.SetToNull)
    {
        if (rowRemovalOption != _rowRemovalOption)
        {
            return this;
        }
        return this;
    }

    public async Task<ReadOnlyMemory<byte>> GetBinaryData(DataElementIdentifier dataElementIdentifier)
    {
        return await _dataMutator.GetBinaryData(dataElementIdentifier);
    }

    public DataElement GetDataElement(DataElementIdentifier dataElementIdentifier)
    {
        return _dataMutator.GetDataElement(dataElementIdentifier);
    }

    public DataType? GetDataType(string dataTypeId)
    {
        return _dataMutator.GetDataType(dataTypeId);
    }
}
