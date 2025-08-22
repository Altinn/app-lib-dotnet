using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Features.Validation.Wrappers;

/// <summary>
/// Wrap the old <see cref="ITaskValidator"/> interface to the new <see cref="IValidator"/> interface.
/// </summary>
internal class TaskValidatorWrapper : IValidator
{
    private readonly ITaskValidator _taskValidator;

    /// <summary>
    /// Constructor that wraps an <see cref="ITaskValidator"/>
    /// </summary>
    public TaskValidatorWrapper(
        /* altinn:injection:ignore */
        ITaskValidator taskValidator
    )
    {
        _taskValidator = taskValidator;
    }

    /// <inheritdoc />
    public string TaskId => _taskValidator.TaskId;

    /// <inheritdoc />
    public string ValidationSource => _taskValidator.ValidationSource;

    /// <inheritdoc />
    public bool NoIncrementalValidation => _taskValidator.NoIncrementalValidation;

    /// <inheritdoc />
    public Task<List<ValidationIssue>> Validate(IInstanceDataAccessor dataAccessor, string taskId, string? language)
    {
        return _taskValidator.ValidateTask(dataAccessor.Instance, taskId, language);
    }

    /// <inheritdoc />
    public Task<bool> HasRelevantChanges(IInstanceDataAccessor dataAccessor, string taskId, DataElementChanges changes)
    {
        // If task validator sets <see cref="ITaskValidator.NoIncrementalValidation"/> to false, it will run on every PATCH request, and because <see cref="ITaskValidator" /> does not have a HasRelevantChanges method,
        // we must run on every PATCH request.
        return Task.FromResult(true);
    }
}
