using Altinn.App.Core.Invokers;

namespace Altinn.App.Core.Receivers;

/// <summary>
/// Interface for implementing a receiver handling instance events.
/// </summary>
public interface IAppEventReceiver
{
    /// <summary>
    /// Callback on first start event of process.
    /// </summary>
    /// <returns></returns>
    public Task OnStartAppEvent(object? sender, AppEventArgs eventArgs);
    
    /// <summary>
    /// Is called when the process for an instance is ended.
    /// </summary>
    public Task OnEndAppEvent(object? sender, AppEventArgs eventArgs);
}
