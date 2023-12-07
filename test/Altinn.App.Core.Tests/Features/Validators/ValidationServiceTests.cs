#nullable enable
using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Validation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Validators;

public class ValidationServiceTests
{
    private class MyModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }
    }

    private static readonly DataElement DefaultDataElement = new()
    {
        DataType = "MyType",
    };

    private readonly Mock<ILogger<ValidationService>> _loggerMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly Mock<IAppModel> _appModelMock = new();
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly ServiceCollection _serviceCollection = new();

    public ValidationServiceTests()
    {
        _serviceCollection.AddSingleton(_loggerMock.Object);
        _serviceCollection.AddSingleton(_dataClientMock.Object);
        _serviceCollection.AddSingleton<IValidationService, ValidationService>();
        _serviceCollection.AddSingleton(_appModelMock.Object);
        _serviceCollection.AddSingleton(_appMetadataMock.Object);
    }

    private class MyNameValidator : GenericFormDataValidator<MyModel>
    {
        public MyNameValidator() : base(DefaultDataElement.DataType)
        {
            RunFor(m => m.Name);
        }

        protected override async Task ValidateFormData(Instance instance, DataElement dataElement, MyModel data, List<string>? changedFields = null)
        {
            if (data.Name != "Ola")
            {
                CreateValidationIssue(m => m.Name, "NameNotOla");
            }
        }
    }

    [Fact]
    public async Task ValidateFormData_WithNoValidators_ReturnsNoErrors()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();

        var validatorService = serviceProvider.GetRequiredService<IValidationService>();
        var data = new MyModel { Name = "Ola" };
        var result = await validatorService.ValidateFormData(new Instance(), DefaultDataElement, null!, data);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateFormData_WithMyNameValidator_ReturnsNoErrorsWhenNameIsOla()
    {
        _serviceCollection.AddSingleton<IFormDataValidator, MyNameValidator>();
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();

        var validatorService = serviceProvider.GetRequiredService<IValidationService>();
        var data = new MyModel { Name = "Ola" };
        var result = await validatorService.ValidateFormData(new Instance(), DefaultDataElement, null!, data);
        result.Should().ContainKey("Altinn.App.Core.Tests.Features.Validators.ValidationServiceTests+MyNameValidator-MyType").WhoseValue.Should().HaveCount(0);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateFormData_WithMyNameValidator_ReturnsErrorsWhenNameIsKari()
    {
        _serviceCollection.AddSingleton<IFormDataValidator, MyNameValidator>();
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();

        var validatorService = serviceProvider.GetRequiredService<IValidationService>();
        var data = new MyModel { Name = "Kari" };
        var result = await validatorService.ValidateFormData(new Instance(), DefaultDataElement, null!, data);
        result.Should().ContainKey("Altinn.App.Core.Tests.Features.Validators.ValidationServiceTests+MyNameValidator-MyType").WhoseValue.Should().ContainSingle().Which.CustomTextKey.Should().Be("NameNotOla");
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateFormData_WithMyNameValidator_ReturnsNoErrorsWhenOnlyAgeIsSoupposedlyChanged()
    {
        _serviceCollection.AddSingleton<IFormDataValidator, MyNameValidator>();
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();

        var validatorService = serviceProvider.GetRequiredService<IValidationService>();
        var data = new MyModel { Name = "Kari" };
        var result = await validatorService.ValidateFormData(new Instance(), DefaultDataElement, null!, data, new List<string> { "age" });
        result.Should()
            .NotContainKey("Altinn.App.Core.Tests.Features.Validators.ValidationServiceTests+MyNameValidator");
        result.Should().HaveCount(0);
    }
}