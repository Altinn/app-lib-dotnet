using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Features.Validation;

/// <summary>
/// Default implementation of the ICopyInstanceValidator interface.
/// This implementation does not do anything to the data
/// </summary>
public class NullCopyInstanceValidator : ICopyInstanceValidator
{
    /// <inheritdoc />
    public Task<InstantiationValidationResult?> Validate(IInstanceDataAccessor sourceInstanceDataAccessor)
    {
        return Task.FromResult((InstantiationValidationResult?)null);
    }
}
