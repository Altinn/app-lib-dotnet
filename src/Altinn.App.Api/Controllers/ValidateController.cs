using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Validation;
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
    private readonly IDataClient _dataClient;
    private readonly IAppModel _appModel;
    private readonly IAppMetadata _appMetadata;
    private readonly IValidationService _validationService;

    /// <summary>
    /// Initialises a new instance of the <see cref="ValidateController"/> class
    /// </summary>
    public ValidateController(
        IInstanceClient instanceClient,
        IValidationService validationService,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IAppModel appModel
    )
    {
        _instanceClient = instanceClient;
        _validationService = validationService;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _appModel = appModel;
    }

    /// <summary>
    /// Validate an app instance. This will validate all individual data elements, both the binary elements and the elements bound
    /// to a model, and then finally the state of the instance.
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">Unique id of the party that is the owner of the instance.</param>
    /// <param name="instanceGuid">Unique id to identify the instance</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    [HttpGet]
    [Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/validate")]
    [ProducesResponseType(typeof(ValidationIssueWithSource), 200)]
    public async Task<IActionResult> ValidateInstance(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? language = null
    )
    {
        Instance? instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        if (instance == null)
        {
            return NotFound();
        }

        string? taskId = instance.Process?.CurrentTask?.ElementId;
        if (taskId == null)
        {
            throw new ValidationException("Unable to validate instance without a started process.");
        }

        try
        {
            var dataAccessor = new CachedInstanceDataAccessor(instance, _dataClient, _appMetadata, _appModel);
            List<ValidationIssueWithSource> messages = await _validationService.ValidateInstanceAtTask(
                instance,
                taskId,
                dataAccessor,
                language
            );
            return Ok(messages);
        }
        catch (PlatformHttpException exception)
        {
            if (exception.Response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return StatusCode(403);
            }

            throw;
        }
    }

    /// <summary>
    /// Validate an app instance. This will validate a single data element
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

        if (instance.Process?.CurrentTask?.ElementId == null)
        {
            throw new ValidationException("Unable to validate instance without a started process.");
        }

        List<ValidationIssueWithSource> messages = new List<ValidationIssueWithSource>();

        DataElement? element = instance.Data.FirstOrDefault(d => d.Id == dataGuid.ToString());

        if (element == null)
        {
            throw new ValidationException("Unable to validate data element.");
        }

        Application application = await _appMetadata.GetApplicationMetadata();

        DataType? dataType = application.DataTypes.FirstOrDefault(et => et.Id == element.DataType);

        if (dataType == null)
        {
            throw new ValidationException("Unknown element type.");
        }

        var dataAccessor = new CachedInstanceDataAccessor(instance, _dataClient, _appMetadata, _appModel);

        // TODO: Consider filtering so that only relevant issues are reported.
        messages.AddRange(
            await _validationService.ValidateInstanceAtTask(instance, dataType.TaskId, dataAccessor, language)
        );

        string taskId = instance.Process.CurrentTask.ElementId;

        // Should this be a BadRequest instead?
        if (!dataType.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase))
        {
            ValidationIssueWithSource message = new ValidationIssueWithSource(
                new ValidationIssue
                {
                    Code = ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                    Severity = ValidationIssueSeverity.Warning,
                    DataElementId = element.Id,
                    Description = $"Data element for task {dataType.TaskId} validated while currentTask is {taskId}",
                    CustomTextKey = ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                    CustomTextParams = new List<string>() { dataType.TaskId, taskId },
                },
                GetType().FullName ?? String.Empty
            );
            messages.Add(message);
        }

        return Ok(messages);
    }
}
