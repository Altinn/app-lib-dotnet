using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation.Wrappers;

public class TaskValidatorWrapper : IValidator
{
    private readonly ITaskValidator _taskValidator;

    public TaskValidatorWrapper(ITaskValidator taskValidator)
    {
        _taskValidator = taskValidator;
    }

    /// <inheritdoc />
    public string TaskId => _taskValidator.TaskId;

    /// <inheritdoc />
    public string ValidationSource => _taskValidator.ValidationSource;

    /// <inheritdoc />
    public Task<List<ValidationIssue>> Validate(
        Instance instance,
        string taskId,
        string? language,
        IInstanceDataAccessor instanceDataAccessor
    )
    {
        return _taskValidator.ValidateTask(instance, taskId, language);
    }
}
