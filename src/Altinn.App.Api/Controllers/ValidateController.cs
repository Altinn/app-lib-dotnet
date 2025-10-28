using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Represents all actions related to validation of data and instances
/// </summary>
[Authorize]
[ApiController]
public class ValidateController : ControllerBase
{
    private readonly IInstanceClient _instanceClient;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly IAppMetadata _appMetadata;
    private readonly IValidationService _validationService;

    /// <summary>
    /// Initialises a new instance of the <see cref="ValidateController"/> class
    /// </summary>
    public ValidateController(
        IInstanceClient instanceClient,
        IValidationService validationService,
        IAppMetadata appMetadata,
        IServiceProvider serviceProvider
    )
    {
        _instanceClient = instanceClient;
        _validationService = validationService;
        _appMetadata = appMetadata;
        _instanceDataUnitOfWorkInitializer = serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();
    }

    /// <summary>
    /// Validate an app instance. This will validate all individual data elements, both the binary elements and the elements bound
    /// to a model, and then finally the state of the instance.
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">Unique id of the party that is the owner of the instance.</param>
    /// <param name="instanceGuid">Unique id to identify the instance</param>
    /// <param name="ignoredValidators">Comma separated list of validators to ignore</param>
    /// <param name="onlyIncrementalValidators">Ignore validators that don't run on PATCH requests</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    [HttpGet]
    [Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/validate")]
    [ProducesResponseType(typeof(List<ValidationIssueWithSource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateInstance(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? ignoredValidators = null,
        [FromQuery] bool? onlyIncrementalValidators = null,
        [FromQuery] string? language = null
    )
    {
        try
        {
            Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);

            string? taskId = instance.Process?.CurrentTask?.ElementId;
            if (taskId == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Validation error",
                    detail: "Unable to validate instance without a started process."
                );
            }

            var dataAccessor = await _instanceDataUnitOfWorkInitializer.Init(instance, taskId, language);

            var ignoredSources = ignoredValidators?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            List<ValidationIssueWithSource> messages = await _validationService.ValidateInstanceAtTask(
                dataAccessor,
                taskId,
                ignoredSources,
                onlyIncrementalValidators,
                language
            );
            return Ok(messages);
        }
        catch (Exception exception)
        {
            var statusCode = exception is PlatformHttpException platformHttpException
                ? (int)platformHttpException.Response.StatusCode
                : StatusCodes.Status500InternalServerError;

            return Problem(
                statusCode: statusCode,
                title: $"Something went wrong.",
                detail: exception.Message
            );
        }
    }

    /// <summary>
    /// Validate a single data element (deprecated).
    /// Use `/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/validate` with filters as needed.
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerId">Unique id of the party that is the owner of the instance.</param>
    /// <param name="instanceId">Unique id to identify the instance</param>
    /// <param name="dataGuid">Unique id identifying specific data element</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    [HttpGet]
    [Obsolete(
        "There is no longer any concept of validating a single data element. Use the /validate endpoint instead."
    )]
    [Route("{org}/{app}/instances/{instanceOwnerId:int}/{instanceId:guid}/data/{dataGuid:guid}/validate")]
    [ProducesResponseType(typeof(List<ValidationIssueWithSource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateData(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerId,
        [FromRoute] Guid instanceId,
        [FromRoute] Guid dataGuid,
        [FromQuery] string? language = null
    )
    {
        Instance? instance = await _instanceClient.GetInstance(app, org, instanceOwnerId, instanceId);
        if (instance == null)
        {
            return NotFound();
        }

        var taskId = instance.Process?.CurrentTask?.ElementId;

        if (taskId is null)
        {
            throw new ValidationException("Unable to validate instance without a started process.");
        }

        List<ValidationIssueWithSource> messages = [];

        DataElement? element = instance.Data.FirstOrDefault(d => d.Id == dataGuid.ToString());

        if (element == null)
        {
            throw new ValidationException("Unable to validate data element.");
        }

        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();

        DataType? dataType = application.DataTypes.FirstOrDefault(et => et.Id == element.DataType);

        if (dataType == null)
        {
            throw new ValidationException("Unknown element type.");
        }

        // Should this be a BadRequest instead?
        // The element will likely not be validated at all if the taskId is not the same as the one in the dataType
        if (!taskId.Equals(dataType.TaskId, StringComparison.OrdinalIgnoreCase))
        {
            ValidationIssueWithSource message = new()
            {
                Code = ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                Severity = ValidationIssueSeverity.Warning,
                DataElementId = element.Id,
                Description = $"Data element for task {dataType.TaskId} validated while currentTask is {taskId}",
                CustomTextKey = ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                CustomTextParams = new List<string>() { dataType.TaskId, taskId },
                Source = GetType().FullName ?? String.Empty,
                NoIncrementalUpdates = true,
            };
            messages.Add(message);
        }

        var dataAccessor = await _instanceDataUnitOfWorkInitializer.Init(instance, dataType.TaskId, language);

        // Run validations for all data elements, but only return the issues for the specific data element
        var issues = await _validationService.ValidateInstanceAtTask(
            dataAccessor,
            dataType.TaskId,
            ignoredValidators: null,
            onlyIncrementalValidators: true,
            language: language
        );
        messages.AddRange(issues.Where(i => i.DataElementId == element.Id));

        return Ok(messages);
    }
}
