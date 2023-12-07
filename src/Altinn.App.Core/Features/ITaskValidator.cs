using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface for handling validation of tasks.
/// </summary>
public interface ITaskValidator
{
    /// <summary>
    /// The task id this validator is for. Typically either hard coded by implementation or
    /// or set by constructor using a <see cref="ServiceKeyAttribute" /> and a keyed service.
    /// </summary>
    /// <example>
    /// <code>
    /// string TaskId { get; init; }
    /// // constructor
    /// public MyTaskValidator([ServiceKey] string taskId)
    /// {
    ///     TaskId = taskId;
    /// }
    /// </code>
    /// </example>
    string TaskId { get; }

    /// <summary>
    /// Unique code for the validator. Used to run partial validations on the backend.
    /// </summary>
    public string Code => this.GetType().FullName ?? string.Empty;

    /// <summary>
    /// Actual validation logic for the task
    /// </summary>
    /// <param name="instance">The instance to validate</param>
    /// <returns>List of validation issues to add to this task validation</returns>
    Task<List<ValidationIssue>> ValidateTask(Instance instance);
}