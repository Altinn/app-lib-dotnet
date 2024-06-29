using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Validation.Wrappers;
using Altinn.App.Core.Internal.App;

namespace Altinn.App.Core.Internal.Validation;

/// <summary>
/// Interface for a factory that can provide validators for a given task or data element.
/// </summary>
public interface IValidatorFactory
{
    /// <summary>
    /// Gets all task validators for a given task.
    /// </summary>
    public IEnumerable<IValidator> GetValidators(string taskId);
}

/// <summary>
/// Implementation of <see cref="IValidatorFactory"/> that takes IEnumerable of validators in constructor from the service provider.
/// </summary>
public class ValidatorFactory : IValidatorFactory
{
    private readonly IEnumerable<ITaskValidator> _taskValidators;
    private readonly IEnumerable<IDataElementValidator> _dataElementValidators;
    private readonly IEnumerable<IFormDataValidator> _formDataValidators;
    private readonly IEnumerable<IValidator> _validators;
    private readonly IAppMetadata _appMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorFactory"/> class.
    /// </summary>
    public ValidatorFactory(
        IEnumerable<ITaskValidator> taskValidators,
        IEnumerable<IDataElementValidator> dataElementValidators,
        IEnumerable<IFormDataValidator> formDataValidators,
        IEnumerable<IValidator> validators,
        IAppMetadata appMetadata
    )
    {
        _taskValidators = taskValidators;
        _dataElementValidators = dataElementValidators;
        _formDataValidators = formDataValidators;
        _validators = validators;
        _appMetadata = appMetadata;
    }

    private IEnumerable<ITaskValidator> GetTaskValidators(string taskId)
    {
        return _taskValidators.Where(tv => tv.TaskId == "*" || tv.TaskId == taskId);
    }

    private IEnumerable<IDataElementValidator> GetDataElementValidators(string dataTypeId)
    {
        return _dataElementValidators.Where(dev => dev.DataType == "*" || dev.DataType == dataTypeId);
    }

    private IEnumerable<IFormDataValidator> GetFormDataValidators(string dataTypeId)
    {
        return _formDataValidators.Where(fdv => fdv.DataType == "*" || fdv.DataType == dataTypeId);
    }

    /// <summary>
    /// Get all validators for a given task. Wrap <see cref="ITaskValidator"/>, <see cref="IDataElementValidator"/> and <see cref="IFormDataValidator"/>
    /// so that they behave as <see cref="IValidator"/>.
    /// </summary>
    public IEnumerable<IValidator> GetValidators(string taskId)
    {
        var validators = new List<IValidator>();
        validators.AddRange(_validators);
        validators.AddRange(GetTaskValidators(taskId).Select(tv => new TaskValidatorWrapper(tv)));
        var dataTypes = _appMetadata.GetApplicationMetadata().Result.DataTypes.Where(dt => dt.TaskId == taskId);
        foreach (var dataType in dataTypes)
        {
            validators.AddRange(
                GetDataElementValidators(dataType.Id).Select(dev => new DataElementValidatorWrapper(dev, dataType))
            );
            validators.AddRange(
                GetFormDataValidators(dataType.Id).Select(fdv => new FormDataValidatorWrapper(fdv, dataType))
            );
        }

        return validators;
    }
}
