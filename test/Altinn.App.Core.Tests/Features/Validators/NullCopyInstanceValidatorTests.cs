using Altinn.App.Core.Features.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Tests.Features.Validators;

public class NullCopyInstanceValidatorTests
{
    [Fact]
    public async Task NullCopyInstanceValidatorTest_Validation_returns_null()
    {
        // Arrange
        var nullCopyInstanceValidator = new NullCopyInstanceValidator();

        // Act
        var result = await nullCopyInstanceValidator.Validate(new Instance());

        // Assert
        Assert.NotNull(result);
    }
}
