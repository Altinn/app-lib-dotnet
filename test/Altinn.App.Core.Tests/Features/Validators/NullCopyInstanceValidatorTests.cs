using Altinn.App.Core.Features.Validation;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Validators;

public class NullCopyInstanceValidatorTests
{
    [Fact]
    public async Task NullCopyInstanceTest_Validation_returns_null()
    {
        // Arrange
        var nullInstantiation = new NullCopyInstanceValidator();

        // Act
        var result = await nullInstantiation.Validate(new Instance());

        // Assert
        Assert.NotNull(result);
    }
}
