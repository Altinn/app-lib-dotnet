using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Null action handler for cases where there is no match on the requested <see cref="IActionHandler"/>
/// </summary>
public class NullActionHandler: IActionHandler
{
    /// <inheritdoc />
    public string Id => "null";

    /// <inheritdoc />
    public Task<bool> HandleAction(Instance instance)
    {
        return Task.FromResult(true);
    }
}