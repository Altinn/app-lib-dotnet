#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Validation;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Validators;

public class GenericValidatorTests
{
    private class MyModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("children")]
        public List<MyModel>? Children { get; set; }
    }

    private class TestValidator : GenericFormDataValidator<MyModel>
    {
        public TestValidator() : base("MyType")
        {
        }

        // Custom method to make the protected RunFor possible to call from the test
        public void RunForExternal(Expression<Func<MyModel, object?>> selector)
        {
            RunFor(selector);
        }

        protected override Task ValidateFormData(Instance instance, DataElement dataElement, MyModel data)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void TestShouldRun()
    {
        var testValidator = new TestValidator();
        testValidator.RunForExternal(m => m.Name);
        testValidator.ShouldRun().Should().BeTrue();
        testValidator.ShouldRun(new List<string>() { "name" }).Should().BeTrue();
        testValidator.ShouldRun(new List<string>() { "age" }).Should().BeFalse();
    }

    [Theory]
    [InlineData("name", false)]
    [InlineData("age", false)]
    [InlineData("children", true)]
    [InlineData("children[0]", true)]
    [InlineData("children[0].age", false)]
    [InlineData("children[2]", false)]
    public void TestShouldRunWithIndexedRow(string changedField, bool shouldBe)
    {
        var testValidator = new TestValidator();
        testValidator.RunForExternal(m => m.Children![0].Name);
        testValidator.ShouldRun(new List<string>() { changedField }).Should().Be(shouldBe);
    }

    [Theory]
    [InlineData("name", false)]
    [InlineData("age", false)]
    [InlineData("children", true)]

    // [InlineData("children[0]", true)] //TODO:Fix
    [InlineData("children[0].age", false)]
    [InlineData("children[2]", false)]
    public void TestShouldRunWithSelectAllRow(string changedField, bool shouldBe)
    {
        var testValidator = new TestValidator();
        testValidator.RunForExternal(m => m.Children!.Select(c => c.Name));
        testValidator.ShouldRun(new List<string>() { changedField }).Should().Be(shouldBe);
    }
}