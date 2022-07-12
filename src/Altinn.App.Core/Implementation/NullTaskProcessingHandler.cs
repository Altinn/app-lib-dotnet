using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Default implementation of the ITaskProcessingHandler interface.
/// This implementation does not do any thing on TaskEnd
/// </summary>
public class NullTaskProcessingHandler: ITaskProcessingHandler
{
    /// <inheritdoc />
    public async Task ProcessTaskEnd(string taskId, Instance instance)
    {
        await Task.CompletedTask;
    }
}