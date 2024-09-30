using Altinn.App.Core.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Internal.Validation;

/// <summary>
/// Interface for a factory that can provide validators for a given task or data element.
/// </summary>
public interface IValidatorFactory
{
    /// <summary>
    /// Gets all task validators for a given task.
    /// </summary>
    public IEnumerable<ITaskValidator> GetTaskValidators(string taskId);

    /// <summary>
    /// Gets all data element validators for a given data element.
    /// </summary>
    public IEnumerable<IDataElementValidator> GetDataElementValidators(string dataTypeId);

    /// <summary>
    /// Gets all form data validators for a given data element.
    /// </summary>
    public IEnumerable<IFormDataValidator> GetFormDataValidators(string dataTypeId);
}

/// <summary>
/// Implementation of <see cref="IValidatorFactory"/> that takes IEnumerable of validators in constructor from the service provider.
/// </summary>
public class ValidatorFactory : IValidatorFactory
{
    private readonly AppImplementationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatorFactory"/> class.
    /// </summary>
    public ValidatorFactory(IServiceProvider sp)
    {
        _factory = sp.GetRequiredService<AppImplementationFactory>();
    }

    /// <inheritdoc />
    public IEnumerable<ITaskValidator> GetTaskValidators(string taskId)
    {
        var validators = _factory.GetAll<ITaskValidator>();
        return validators.Where(tv => tv.TaskId == "*" || tv.TaskId == taskId);
    }

    /// <inheritdoc />
    public IEnumerable<IDataElementValidator> GetDataElementValidators(string dataTypeId)
    {
        var validators = _factory.GetAll<IDataElementValidator>();
        return validators.Where(dev => dev.DataType == "*" || dev.DataType == dataTypeId);
    }

    /// <inheritdoc />
    public IEnumerable<IFormDataValidator> GetFormDataValidators(string dataTypeId)
    {
        var validators = _factory.GetAll<IFormDataValidator>();
        return validators.Where(fdv => fdv.DataType == "*" || fdv.DataType == dataTypeId);
    }
}
