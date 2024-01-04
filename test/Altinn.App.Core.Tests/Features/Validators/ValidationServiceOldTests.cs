#nullable enable
using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Validation;
using Altinn.App.Core.Features.Validation.Default;
using Altinn.App.Core.Features.Validation.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Validators;

public class ValidationServiceOldTests
{
    private readonly Mock<ILogger<ValidationService>> _loggerMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly Mock<IAppModel> _appModelMock = new();
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly ServiceCollection _serviceCollection = new();

    private readonly ApplicationMetadata _applicationMetadata = new("tdd/test")
    {
        DataTypes = new List<DataType>()
        {
            new DataType()
            {
                Id = "test",
                TaskId = "Task_1",
                EnableFileScan = false,
                ValidationErrorOnPendingFileScan = false,
            }
        }
    };

    public ValidationServiceOldTests()
    {
        _serviceCollection.AddSingleton(_loggerMock.Object);
        _serviceCollection.AddSingleton(_dataClientMock.Object);
        _serviceCollection.AddSingleton<IValidationService, ValidationService>();
        _serviceCollection.AddSingleton(_appModelMock.Object);
        _serviceCollection.AddSingleton(_appMetadataMock.Object);
        _serviceCollection.AddSingleton<IDataElementValidator, DefaultDataElementValidator>();
        _appMetadataMock.Setup(am => am.GetApplicationMetadata()).ReturnsAsync(_applicationMetadata);
    }

    [Fact]
    public async Task FileScanEnabled_VirusFound_ValidationShouldFail()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();
        IValidationService validationService = serviceProvider.GetRequiredService<IValidationService>();

        var instance = new Instance();
        var dataType = new DataType() { EnableFileScan = true };
        var dataElement = new DataElement() 
        {
            DataType = "test",
            FileScanResult = FileScanResult.Infected
        };

        List<ValidationIssue> validationIssues = await validationService.ValidateDataElement(instance, dataElement, dataType);

        validationIssues.FirstOrDefault(vi => vi.Code == "DataElementFileInfected").Should().NotBeNull();
    }

    [Fact]
    public async Task FileScanEnabled_PendingScanNotEnabled_ValidationShouldNotFail()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();
        IValidationService validationService = serviceProvider.GetRequiredService<IValidationService>();

        var dataType = new DataType()
            { Id = "test", TaskId = "Task_1", AppLogic = null, EnableFileScan = true };
        var instance = new Instance()
        {
        };
        var dataElement = new DataElement()
        {
            DataType = "test",
            FileScanResult = FileScanResult.Pending,
        };

        List<ValidationIssue> validationIssues = await validationService.ValidateDataElement(instance, dataElement, dataType);

        validationIssues.FirstOrDefault(vi => vi.Code == "DataElementFileScanPending").Should().BeNull();
    }

    [Fact]
    public async Task FileScanEnabled_PendingScanEnabled_ValidationShouldNotFail()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();
        IValidationService validationService = serviceProvider.GetRequiredService<IValidationService>();

        var instance = new Instance();
        var dataType = new DataType() { EnableFileScan = true, ValidationErrorOnPendingFileScan = true };
        var dataElement = new DataElement()
        {
            DataType = "test",
            FileScanResult = FileScanResult.Pending
        };

        List<ValidationIssue> validationIssues = await validationService.ValidateDataElement(instance, dataElement, dataType);

        validationIssues.FirstOrDefault(vi => vi.Code == "DataElementFileScanPending").Should().NotBeNull();
    }

    [Fact]
    public async Task FileScanEnabled_Clean_ValidationShouldNotFail()
    {
        await using var serviceProvider = _serviceCollection.BuildServiceProvider();
        IValidationService validationService = serviceProvider.GetRequiredService<IValidationService>();

        var instance = new Instance();
        var dataType = new DataType() { EnableFileScan = true, ValidationErrorOnPendingFileScan = true };
        var dataElement = new DataElement()
        {
            DataType = "test",
            FileScanResult = FileScanResult.Clean,
        };

        List<ValidationIssue> validationIssues = await validationService.ValidateDataElement(instance, dataElement, dataType);

        validationIssues.FirstOrDefault(vi => vi.Code == "DataElementFileInfected").Should().BeNull();
        validationIssues.FirstOrDefault(vi => vi.Code == "DataElementFileScanPending").Should().BeNull();
    }

    [Fact]
    public void ModelKeyToField_NullInputWithoutType_ReturnsNull()
    {
        ModelStateHelpers.ModelKeyToField(null, null!).Should().BeNull();
    }

    [Fact]
    public void ModelKeyToField_StringInputWithoutType_ReturnsSameString()
    {
        ModelStateHelpers.ModelKeyToField("null", null!).Should().Be("null");
    }

    [Fact]
    public void ModelKeyToField_NullInput_ReturnsNull()
    {
        ModelStateHelpers.ModelKeyToField(null, typeof(TestModel)).Should().BeNull();
    }

    [Fact]
    public void ModelKeyToField_StringInput_ReturnsSameString()
    {
        ModelStateHelpers.ModelKeyToField("null", typeof(TestModel)).Should().Be("null");
    }
    
    [Fact]
    public void ModelKeyToField_StringInputWithAttr_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("FirstLevelProp", typeof(TestModel)).Should().Be("level1");
    }
    
    [Fact]
    public void ModelKeyToField_SubModel_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModel.DecimalNumber", typeof(TestModel)).Should().Be("sub.decimal");
    }

    [Fact]
    public void ModelKeyToField_SubModelNullable_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModel.StringNullable", typeof(TestModel)).Should().Be("sub.nullableString");
    }

    [Fact]
    public void ModelKeyToField_SubModelWithSubmodel_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModel.StringNullable", typeof(TestModel)).Should().Be("sub.nullableString");
    }

    [Fact]
    public void ModelKeyToField_SubModelNull_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelNull.DecimalNumber", typeof(TestModel)).Should().Be("subnull.decimal");
    }

    [Fact]
    public void ModelKeyToField_SubModelNullNullable_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelNull.StringNullable", typeof(TestModel)).Should().Be("subnull.nullableString");
    }

    [Fact]
    public void ModelKeyToField_SubModelNullWithSubmodel_ReturnsMappedString()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelNull.StringNullable", typeof(TestModel)).Should().Be("subnull.nullableString");
    }

    // Test lists
    [Fact]
    public void ModelKeyToField_List_IgnoresMissingIndex()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList.StringNullable", typeof(TestModel)).Should().Be("subList.nullableString");
    }

    [Fact]
    public void ModelKeyToField_List_ProxiesIndex()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList[123].StringNullable", typeof(TestModel)).Should().Be("subList[123].nullableString");
    }

    [Fact]
    public void ModelKeyToField_ListOfList_ProxiesIndex()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList[123].ListOfDecimal[5]", typeof(TestModel)).Should().Be("subList[123].decimalList[5]");
    }

    [Fact]
    public void ModelKeyToField_ListOfList_IgnoresMissing()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList[123].ListOfDecimal", typeof(TestModel)).Should().Be("subList[123].decimalList");
    }

    [Fact]
    public void ModelKeyToField_ListOfListNullable_IgnoresMissing()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList[123].ListOfNullableDecimal", typeof(TestModel)).Should().Be("subList[123].nullableDecimalList");
    }

    [Fact]
    public void ModelKeyToField_ListOfListOfListNullable_IgnoresMissingButPropagatesOthers()
    {
        ModelStateHelpers.ModelKeyToField("SubTestModelList[123].SubTestModelList.ListOfNullableDecimal[123456]", typeof(TestModel)).Should().Be("subList[123].subList.nullableDecimalList[123456]");
    }

    public class TestModel
    {
        [JsonPropertyName("level1")]
        public string FirstLevelProp { get; set; } = default!;

        [JsonPropertyName("sub")]
        public SubTestModel SubTestModel { get; set; } = default!;

        [JsonPropertyName("subnull")]
        public SubTestModel? SubTestModelNull { get; set; } = default!;

        [JsonPropertyName("subList")]
        public List<SubTestModel> SubTestModelList { get; set; } = default!;
    }

    public class SubTestModel
    {
        [JsonPropertyName("decimal")]
        public decimal DecimalNumber { get; set; } = default!;

        [JsonPropertyName("nullableString")]
        public string? StringNullable { get; set; } = default!;

        [JsonPropertyName("decimalList")]
        public List<decimal> ListOfDecimal { get; set; } = default!;

        [JsonPropertyName("nullableDecimalList")]
        public List<decimal?> ListOfNullableDecimal { get; set; } = default!;

        [JsonPropertyName("subList")]
        public List<SubTestModel> SubTestModelList { get; set; } = default!;
    }
}
