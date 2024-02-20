using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Null implementation. Used when no other <see cref="IProcessTask"/> can be found
/// </summary>
public class NullTypeProcessTask : IProcessTask
{

    /// <inheritdoc/>
    public string Type => "NullType";

    /// <inheritdoc/>
    public async Task Start(string elementId, Instance instance, Dictionary<string, string> prefill)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task End(string elementId, Instance instance)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Abandon(string elementId, Instance instance)
    {
        await Task.CompletedTask;
    }
}