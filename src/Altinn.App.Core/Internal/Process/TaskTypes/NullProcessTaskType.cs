using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.TaskTypes;

/// <summary>
/// Null implementation. Used when no other <see cref="IProcessTaskType"/> can be found
/// </summary>
public class NullProcessTaskType: IProcessTaskType
{
    
    /// <inheritdoc/>
    public string Key => "NullType";
    
    /// <inheritdoc/>
    public async Task HandleTaskStart(string elementId, Instance instance, Dictionary<string, string> prefill)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task HandleTaskComplete(string elementId, Instance instance)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task HandleTaskAbandon(string elementId, Instance instance)
    {
        await Task.CompletedTask;
    }
}
