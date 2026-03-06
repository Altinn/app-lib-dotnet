using System.Diagnostics;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.WorkflowEngine;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling process engine callbacks.
/// </summary>
[ApiController]
// [Authorize(AuthenticationSchemes = "X-Api-Key")]
[AllowAnonymous]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/workflow-engine-callbacks")]
public class WorkflowEngineCallbackController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InstanceStateService _instanceStateService;
    private readonly ILogger<WorkflowEngineCallbackController> _logger;
    private readonly Telemetry? _telemetry;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngineCallbackController"/> class.
    /// </summary>
    public WorkflowEngineCallbackController(
        IServiceProvider serviceProvider,
        ILogger<WorkflowEngineCallbackController> logger,
        Telemetry? telemetry = null
    )
    {
        _serviceProvider = serviceProvider;
        _instanceStateService = serviceProvider.GetRequiredService<InstanceStateService>();
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Executes a command based on the provided command key.
    /// </summary>
    [HttpPost("{commandKey}")]
    public async Task<IActionResult> ExecuteCommand(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string commandKey,
        [FromBody] AppCallbackPayload payload,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = _telemetry?.StartProcessEngineCallbackActivity(instanceGuid, commandKey);

        var appId = new AppIdentifier(org, app);
        var instanceId = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        IWorkflowEngineCommand? command = _serviceProvider
            .GetServices<IWorkflowEngineCommand>()
            .FirstOrDefault(x => x.GetKey() == commandKey);

        if (command is null)
        {
            string commandNotFoundError = $"Workflow app command '{commandKey}' not found. Instance: {instanceId}.";
            _logger.LogError(commandNotFoundError);
            activity?.SetStatus(ActivityStatusCode.Error, commandNotFoundError);
            return NonRetryableProblem("Command Not Found", commandNotFoundError, StatusCodes.Status404NotFound);
        }

        // Restore instance + form data from the opaque state blob.
        // State must always be provided — every workflow is enqueued with a captured state blob.
        if (payload.State is null)
        {
            string missingStateError =
                $"State blob is missing from callback payload. CommandKey: {commandKey}, Instance: {instanceId}.";
            _logger.LogError(missingStateError);
            activity?.SetStatus(ActivityStatusCode.Error, missingStateError);
            return NonRetryableProblem("Missing State", missingStateError, StatusCodes.Status422UnprocessableEntity);
        }

        InstanceDataUnitOfWork instanceDataUnitOfWork = await _instanceStateService.RestoreState(
            payload.State,
            payload.Actor.Language
        );

        string? currentTaskId = instanceDataUnitOfWork.Instance.Process?.CurrentTask?.ElementId;

        ProcessEngineCommandResult result = await command.Execute(
            new ProcessEngineCommandContext
            {
                AppId = appId,
                InstanceId = instanceId,
                InstanceDataMutator = instanceDataUnitOfWork,
                CancellationToken = cancellationToken,
                Payload = payload,
            }
        );

        //TODO: Consider rewriting IInstanceDataMutator so that we can construct one that doesn't allow abandonment in this scenario. Don't think it makes sense when the process engine is the caller.
        if (instanceDataUnitOfWork.HasAbandonIssues)
        {
            string message =
                $"Data abandonment detected during callback. CommandKey: {commandKey}, Instance: {instanceId}, Task: {currentTaskId}";

            _logger.LogError(message, commandKey, instanceId, currentTaskId);

            activity?.SetStatus(ActivityStatusCode.Error, message);

            return NonRetryableProblem("Data Abandonment", message, StatusCodes.Status422UnprocessableEntity);
        }

        switch (result)
        {
            case SuccessfulProcessEngineCommandResult success:
                DataElementChanges changes = instanceDataUnitOfWork.GetDataElementChanges(false);
                await instanceDataUnitOfWork.UpdateInstanceData(changes);
                await instanceDataUnitOfWork.SaveChanges(changes);

                // Capture updated state (includes Storage-assigned IDs for newly created data elements)
                string updatedState = await _instanceStateService.CaptureState(instanceDataUnitOfWork);

                // If the command signals auto-advance, enqueue a dependent process-next workflow.
                // This happens AFTER save so the state blob includes Storage-assigned IDs.
                // If this fails, we return 500 — the engine retries the whole callback (at-least-once).
                // The enqueue uses an idempotency key, so duplicates are safe.
                if (success.AutoAdvanceProcess)
                {
                    var processEngine = _serviceProvider.GetRequiredService<IProcessEngine>();
                    await processEngine.EnqueueProcessNext(
                        instanceDataUnitOfWork.Instance,
                        payload.Actor,
                        payload.LockToken,
                        payload.WorkflowId,
                        updatedState,
                        success.AutoAdvanceAction,
                        cancellationToken
                    );
                }

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Ok(new AppCallbackResponse { State = updatedState });

            case FailedProcessEngineCommandResult failed:
                _logger.LogError(
                    "Callback handler failed. CommandKey: {CommandKey}, Instance: {InstanceId}, Task: {TaskId}, Error: {ErrorMessage}, ExceptionType: {ExceptionType}",
                    commandKey,
                    instanceId,
                    currentTaskId,
                    failed.ErrorMessage,
                    failed.ExceptionType
                );
                activity?.SetStatus(ActivityStatusCode.Error, failed.ErrorMessage);

                if (failed.NonRetryable)
                {
                    return NonRetryableProblem(
                        failed.ExceptionType,
                        failed.ErrorMessage,
                        StatusCodes.Status422UnprocessableEntity
                    );
                }

                return Problem(
                    title: failed.ExceptionType,
                    detail: failed.ErrorMessage,
                    statusCode: StatusCodes.Status500InternalServerError
                );

            default:
                _logger.LogError(
                    "Unexpected callback result type: {ResultType}. CommandKey: {CommandKey}, Instance: {InstanceId}",
                    result.GetType().Name,
                    commandKey,
                    instanceId
                );
                throw new InvalidOperationException($"Unexpected result type: {result.GetType().Name}");
        }
    }

    private static ObjectResult NonRetryableProblem(string title, string detail, int statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
        };
        problemDetails.Extensions["nonRetryable"] = true;
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
