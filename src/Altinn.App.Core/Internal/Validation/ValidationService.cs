using System.Globalization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Validation;

/// <summary>
/// Main validation service that encapsulates all validation logic
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IValidatorFactory _validatorFactory;
    private readonly IDataClient _dataClient;
    private readonly IAppModel _appModel;
    private readonly IAppMetadata _appMetadata;
    private readonly ILogger<ValidationService> _logger;
    private readonly Telemetry? _telemetry;

    /// <summary>
    /// Constructor with DI services
    /// </summary>
    public ValidationService(
        IValidatorFactory validatorFactory,
        IDataClient dataClient,
        IAppModel appModel,
        IAppMetadata appMetadata,
        ILogger<ValidationService> logger,
        Telemetry? telemetry = null
    )
    {
        _validatorFactory = validatorFactory;
        _dataClient = dataClient;
        _appModel = appModel;
        _appMetadata = appMetadata;
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    public async Task<List<ValidationIssueWithSource>> ValidateInstanceAtTask(
        Instance instance,
        string taskId,
        IInstanceDataAccessor dataAccessor,
        string? language
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(taskId);

        using var activity = _telemetry?.StartValidateInstanceAtTaskActivity(instance, taskId);

        // Run task validations (but don't await yet)
        var validators = _validatorFactory.GetValidators(taskId);
        var validationTasks = validators.Select(async v =>
        {
            using var validatorActivity = _telemetry?.StartRunValidatorActivity(v);
            try
            {
                var issues = await v.Validate(instance, taskId, language, dataAccessor);
                return KeyValuePair.Create(
                    v.ValidationSource,
                    issues.Select(issue => new ValidationIssueWithSource(issue, v.ValidationSource))
                );
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error while running validator {validatorName} for task {taskId} on instance {instanceId}",
                    v.ValidationSource,
                    taskId,
                    instance.Id
                );
                validatorActivity?.Errored(e);
                throw;
            }
        });
        var lists = await Task.WhenAll(validationTasks);

        // Flatten the list of lists to a single list of issues
        return lists.SelectMany(x => x.Value).ToList();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<ValidationIssueWithSource>>> ValidateIncrementalFormData(
        Instance instance,
        string taskId,
        List<DataElementChange> changes,
        IInstanceDataAccessor dataAccessor,
        List<string>? ignoredValidators,
        string? language
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(taskId);
        ArgumentNullException.ThrowIfNull(changes);

        using var activity = _telemetry?.StartValidateIncrementalActivity(instance, taskId, changes);

        // Run task validations (but don't await yet)
        var validators = _validatorFactory.GetValidators(taskId);
        var validationTasks = validators.Select(async validator =>
        {
            using var validatorActivity = _telemetry?.StartRunValidatorActivity(validator);
            try
            {
                var hasRelevantChanges = await validator.HasRelevantChanges(instance, taskId, changes, dataAccessor);
                validatorActivity?.SetTag(Telemetry.InternalLabels.ValidatorRelevantChanges, hasRelevantChanges);
                if (hasRelevantChanges)
                {
                    var issues = await validator.Validate(instance, taskId, language, dataAccessor);
                    var issuesWithSource = issues
                        .Select(i => new ValidationIssueWithSource(i, validator.ValidationSource))
                        .ToList();
                    return KeyValuePair.Create(validator.ValidationSource, issuesWithSource);
                }

                return KeyValuePair.Create(validator.ValidationSource, new List<ValidationIssueWithSource>());
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error while running validator {validatorName} on task {taskId} in instance {instanceId}",
                    validator.GetType().Name,
                    taskId,
                    instance.Id
                );
                validatorActivity?.Errored(e);
                throw;
            }
        });

        var lists = await Task.WhenAll(validationTasks);

        return lists.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
