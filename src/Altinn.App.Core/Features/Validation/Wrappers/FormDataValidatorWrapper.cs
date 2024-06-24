namespace Altinn.App.Core.Features.Validation.Wrappers;

using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

public class FormDataValidatorWrapper : IValidator, IIncrementalValidator
{
    private readonly IFormDataValidator _dataElementValidator;
    private readonly DataType _dataType;

    public FormDataValidatorWrapper(IFormDataValidator dataElementValidator, DataType dataType)
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
            var data = await instanceDataAccessor.Get(instance, dataElement);
            var dataElementValidationResult = await _dataElementValidator.ValidateFormData(
                instance,
                dataElement,
                data,
                language
            );
            issues.AddRange(dataElementValidationResult);
        }

        return issues;
    }

    /// <inheritdoc />
    public async Task<bool> HasRelevantChanges(
        Instance instance,
        string taskId,
        string? language,
        List<DataElementChange> changes,
        IInstanceDataAccessor instanceDataAccessor
    )
    {
        if (changes.All(c => c.DataElement.DataType != _dataType.Id))
        {
            return false;
        }

        foreach (var change in changes)
        {
            if (
                _dataElementValidator.DataType == change.DataElement.DataType
                && _dataElementValidator.HasRelevantChanges(change.CurrentValue, change.PreviousValue)
            )
            {
                return true;
            }
        }

        return false;
    }
}
