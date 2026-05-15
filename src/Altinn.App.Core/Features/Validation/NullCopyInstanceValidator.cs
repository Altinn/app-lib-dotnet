using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation;

/// <summary>
/// Default implementation of the ICopyInstanceValidator interface.
/// This implementation does not do any thing to the data
/// </summary>
public class NullCopyInstanceValidator : ICopyInstanceValidator
{
    /// <inheritdoc />
    public async Task<InstantiationValidationResult?> Validate(Instance sourceInstance)
    {
        return await Task.FromResult((InstantiationValidationResult?)null);
    }
}
