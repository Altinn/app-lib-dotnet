using Altinn.App.Core.Interface;
using Altinn.App.Services.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Default implementation of the IInstantiation interface.
/// This implementation does not do any thing to the data
/// </summary>
public class NullInstantiation: IInstantiation
{
    /// <inheritdoc />
    public async Task<InstantiationValidationResult> Validation(Instance instance)
    {
        return await Task.FromResult((InstantiationValidationResult)null);
    }

    /// <inheritdoc />
    public async Task DataCreation(Instance instance, object data, Dictionary<string, string> prefill)
    {
        await Task.CompletedTask;
    }
}