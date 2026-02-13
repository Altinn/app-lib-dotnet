using System.Diagnostics;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.WorkflowEngine.Commands;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
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
    private readonly IInstanceClient _instanceClient;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly ILogger<WorkflowEngineCallbackController> _logger;
    private readonly Telemetry? _telemetry;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowEngineCallbackController"/> class.
    /// </summary>
    public WorkflowEngineCallbackController(
        IServiceProvider serviceProvider,
        IInstanceClient instanceClient,
        ILogger<WorkflowEngineCallbackController> logger,
        Telemetry? telemetry = null
    )
    {
        _serviceProvider = serviceProvider;
        _instanceClient = instanceClient;
        _instanceDataUnitOfWorkInitializer = serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();
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
            var commandNotFoundError = $"Workflow app command '{commandKey}' not found. Instance: {instanceId}.";
            _logger.LogError(commandNotFoundError);
            activity?.SetStatus(ActivityStatusCode.Error, commandNotFoundError);
            return NotFound();
        }

        Instance instance = await _instanceClient.GetInstance(
            app,
            org,
            instanceOwnerPartyId,
            instanceId.InstanceGuid,
            StorageAuthenticationMethod.ServiceOwner(),
            cancellationToken
        );

        string? currentTaskId = instance.Process?.CurrentTask?.ElementId;

        InstanceDataUnitOfWork instanceDataUnitOfWork = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            taskId: currentTaskId,
            payload.Actor.Language,
            StorageAuthenticationMethod.ServiceOwner()
        );

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
            var message =
                $"Data abandonment detected during callback. CommandKey: {commandKey}, Instance: {instanceId}, Task: {currentTaskId}";

            _logger.LogError(message, commandKey, instanceId, currentTaskId);

            activity?.SetStatus(ActivityStatusCode.Error, message);

            return BadRequest(
                new CallbackErrorResponse { Message = message, ExceptionType = "DataAbandonmentException" }
            );
        }

        switch (result)
        {
            case SuccessfulProcessEngineCommandResult:
                DataElementChanges changes = instanceDataUnitOfWork.GetDataElementChanges(false);
                await instanceDataUnitOfWork.UpdateInstanceData(changes);
                await instanceDataUnitOfWork.SaveChanges(changes);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Ok();

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
                return BadRequest(
                    new CallbackErrorResponse { Message = failed.ErrorMessage, ExceptionType = failed.ExceptionType }
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
}
