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

    /// <summary>
    /// Constructor with DI services
    /// </summary>
    public ValidationService(
        IValidatorFactory validatorFactory,
        IDataClient dataClient,
        IAppModel appModel,
        IAppMetadata appMetadata,
        ILogger<ValidationService> logger
    )
    {
        _validatorFactory = validatorFactory;
        _dataClient = dataClient;
        _appModel = appModel;
        _appMetadata = appMetadata;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ValidationIssue>> ValidateInstanceAtTask(Instance instance, string taskId, string? language)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(taskId);

        // Run task validations (but don't await yet)
        Task<List<ValidationIssue>[]> taskIssuesTask = RunTaskValidators(instance, taskId, language);

        // Get list of data elements for the task
        var application = await _appMetadata.GetApplicationMetadata();
        var dataTypesForTask = application.DataTypes.Where(dt => dt.TaskId == taskId).ToList();
        var dataElementsToValidate = instance
            .Data.Where(de => dataTypesForTask.Exists(dt => dt.Id == de.DataType))
            .ToArray();
        // Run ValidateDataElement for each data element (but don't await yet)
        var dataIssuesTask = Task.WhenAll(
            dataElementsToValidate.Select(dataElement =>
                ValidateDataElement(
                    instance,
                    dataElement,
                    dataTypesForTask.First(dt => dt.Id == dataElement.DataType),
                    language
                )
            )
        );

        List<ValidationIssue>[][] lists = await Task.WhenAll(taskIssuesTask, dataIssuesTask);
        // Flatten the list of lists to a single list of issues
        return lists.SelectMany(x => x.SelectMany(y => y)).ToList();
    }

    private Task<List<ValidationIssue>[]> RunTaskValidators(Instance instance, string taskId, string? language)
    {
        var taskValidators = _validatorFactory.GetTaskValidators(taskId);

        return Task.WhenAll(
            taskValidators.Select(async tv =>
            {
                try
                {
                    _logger.LogDebug(
                        "Start running validator {validatorName} on task {taskId} in instance {instanceId}",
                        tv.GetType().Name,
                        taskId,
                        instance.Id
                    );
                    var issues = await tv.ValidateTask(instance, taskId, language);
                    issues.ForEach(i => i.Source = tv.ValidationSource); // Ensure that the source is set to the validator source
                    return issues;
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Error while running validator {validatorName} on task {taskId} in instance {instanceId}",
                        tv.GetType().Name,
                        taskId,
                        instance.Id
                    );
                    throw;
                }
            })
        );
    }

    /// <inheritdoc/>
    public async Task<List<ValidationIssue>> ValidateDataElement(
        Instance instance,
        DataElement dataElement,
        DataType dataType,
        string? language
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(dataElement);
        ArgumentNullException.ThrowIfNull(dataType);
        ArgumentNullException.ThrowIfNull(dataElement.DataType);

        // Get both keyed and non-keyed validators for the data type
        Task<List<ValidationIssue>[]> dataElementsIssuesTask = RunDataElementValidators(
            instance,
            dataElement,
            dataType,
            language
        );

        // Run extra validation on form data elements with app logic
        if (dataType.AppLogic?.ClassRef is not null)
        {
            Type modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);

            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            string app = instance.AppId.Split("/")[1];
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
            var data = await _dataClient.GetFormData(
                instanceGuid,
                modelType,
                instance.Org,
                app,
                instanceOwnerPartyId,
                Guid.Parse(dataElement.Id)
            ); // TODO: Add method that accepts instance and dataElement
            var formDataIssuesDictionary = await ValidateFormData(
                instance,
                dataElement,
                dataType,
                data,
                previousData: null,
                ignoredValidators: null,
                language
            );

            return (await dataElementsIssuesTask)
                .SelectMany(x => x)
                .Concat(formDataIssuesDictionary.SelectMany(kv => kv.Value))
                .ToList();
        }

        return (await dataElementsIssuesTask).SelectMany(x => x).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<ValidationIssue>> ValidateFileUpload(
        Instance instance,
        DataType dataType,
        byte[] fileContent,
        string? filename,
        string? mimeType,
        string? language
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(dataType);
        ArgumentNullException.ThrowIfNull(fileContent);

        var validators = _validatorFactory.GetFileUploadValidators(dataType.Id);
        var issuesLists = await Task.WhenAll(
            validators.Select(async v =>
            {
                try
                {
                    _logger.LogDebug(
                        "Start running validator {validatorName} on {dataType} for instance {instanceId}",
                        v.GetType().Name,
                        dataType.Id,
                        instance.Id
                    );
                    var issues = await v.Validate(instance, dataType, fileContent, filename, mimeType, language);
                    issues.ForEach(i => i.Source = v.ValidationSource); // Ensure that the source is set to the validator source
                    return issues;
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Error while running validator {validatorName} on {dataType} for instance {instanceId}",
                        v.GetType().Name,
                        dataType.Id,
                        instance.Id
                    );
                    throw;
                }
            })
        );

        return issuesLists.SelectMany(l => l).ToList();
    }

    private Task<List<ValidationIssue>[]> RunDataElementValidators(
        Instance instance,
        DataElement dataElement,
        DataType dataType,
        string? language
    )
    {
        var validators = _validatorFactory.GetDataElementValidators(dataType.Id);

        var dataElementsIssuesTask = Task.WhenAll(
            validators.Select(async v =>
            {
                try
                {
                    _logger.LogDebug(
                        "Start running validator {validatorName} on {dataType} for data element {dataElementId} in instance {instanceId}",
                        v.GetType().Name,
                        dataElement.DataType,
                        dataElement.Id,
                        instance.Id
                    );
                    var issues = await v.ValidateDataElement(instance, dataElement, dataType, language);
                    issues.ForEach(i => i.Source = v.ValidationSource); // Ensure that the source is set to the validator source
                    return issues;
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "Error while running validator {validatorName} on {dataType} for data element {dataElementId} in instance {instanceId}",
                        v.GetType().Name,
                        dataElement.DataType,
                        dataElement.Id,
                        instance.Id
                    );
                    throw;
                }
            })
        );

        return dataElementsIssuesTask;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<ValidationIssue>>> ValidateFormData(
        Instance instance,
        DataElement dataElement,
        DataType dataType,
        object data,
        object? previousData,
        List<string>? ignoredValidators,
        string? language
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(dataElement);
        ArgumentNullException.ThrowIfNull(dataElement.DataType);
        ArgumentNullException.ThrowIfNull(data);

        // Locate the relevant data validator services from normal and keyed services
        var dataValidators = _validatorFactory
            .GetFormDataValidators(dataType.Id)
            .Where(dv => ignoredValidators?.Contains(dv.ValidationSource) != true) // Filter out ignored validators
            .Where(dv => previousData is null || dv.HasRelevantChanges(data, previousData))
            .ToArray();

        var issuesLists = await Task.WhenAll(
            dataValidators.Select(
                async (v) =>
                {
                    try
                    {
                        _logger.LogDebug(
                            "Start running validator {validatorName} on {dataType} for data element {dataElementId} in instance {instanceId}",
                            v.GetType().Name,
                            dataElement.DataType,
                            dataElement.Id,
                            instance.Id
                        );
                        var issues = await v.ValidateFormData(instance, dataElement, data, language);
                        issues.ForEach(i => i.Source = v.ValidationSource); // Ensure that the Source is set to the ValidatorSource
                        return issues;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            e,
                            "Error while running validator {validatorName} on {dataType} for data element {dataElementId} in instance {instanceId}",
                            v.GetType().Name,
                            dataElement.DataType,
                            dataElement.Id,
                            instance.Id
                        );
                        throw;
                    }
                }
            )
        );

        return dataValidators.Zip(issuesLists).ToDictionary(kv => kv.First.ValidationSource, kv => kv.Second);
    }
}
