using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation.Wrappers;

public class DataElementValidatorWrapper : IValidator
{
    private readonly IDataElementValidator _dataElementValidator;
    private readonly DataType _dataType;

    public DataElementValidatorWrapper(IDataElementValidator dataElementValidator, DataType dataType)
    {
        _dataElementValidator = dataElementValidator;
        _dataType = dataType;
    }

    /// <inheritdoc />
    public string TaskId => _dataType.TaskId;

    /// <inheritdoc />
    public string ValidationSource => _dataElementValidator.ValidationSource;

    /// <summary>
    /// Run all legacy <see cref="IDataElementValidator"/> instances for the given <see cref="DataType"/>.
    /// </summary>
    public async Task<List<ValidationIssue>> Validate(
        Instance instance,
        string taskId,
        string? language,
        IInstanceDataAccessor instanceDataAccessor
    )
    {
        var issues = new List<ValidationIssue>();
        foreach (var dataElement in instance.Data.Where(d => d.DataType == _dataElementValidator.DataType))
        {
            var dataElementValidationResult = await _dataElementValidator.ValidateDataElement(
                instance,
                dataElement,
                _dataType,
                language
            );
            issues.AddRange(dataElementValidationResult);
        }

        return issues;
    }
}
