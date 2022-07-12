using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Default implementation of the IDataProcessingHandler interface.
/// This implementation does not do any thing to the data
/// </summary>
public class NullDataProcessingHandler: IDataProcessingHandler
{
    /// <inheritdoc />
    public async Task<bool> ProcessDataRead(Instance instance, Guid? dataId, object data)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<bool> ProcessDataWrite(Instance instance, Guid? dataId, object data)
    {
        return await Task.FromResult(false);
    }
}