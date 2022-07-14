using System.Collections.Generic;
using Altinn.App.Core.Implementation;
using Altinn.App.PlatformServices.Tests.Implementation.TestResources;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Implementation;

public class NullInstantiationTests
{
    [Fact]
    public async void NullInstantiationTest_Validation_returns_null()
    {
        // Arrange
        var nullInstantiation = new NullInstantiation();

        // Act
        var result = await nullInstantiation.Validation(new Instance());

        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async void NullInstantiationTest_DataCreation_changes_nothing()
    {
        // Arrange
        var nullInstantiation = new NullInstantiation();
        DummyModel expected = new DummyModel()
        {
            Name = "Test",
        };
        object input = new DummyModel()
        {
            Name = "Test"
        };

        // Act
        await nullInstantiation.DataCreation(new Instance(), input, new Dictionary<string, string>());

        // Assert
        input.Should().BeEquivalentTo(expected);
    }
}