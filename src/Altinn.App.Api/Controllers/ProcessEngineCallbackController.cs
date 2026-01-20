using System.Diagnostics;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.Core.Models;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling process engine callbacks.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKeySchemeName)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine-callbacks")]
public class ProcessEngineCallbackController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInstanceClient _instanceClient;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly ILogger<ProcessEngineCallbackController> _logger;
    private readonly Telemetry? _telemetry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineCallbackController"/> class.
    /// </summary>
    public ProcessEngineCallbackController(
        IServiceProvider serviceProvider,
        IInstanceClient instanceClient,
        ILogger<ProcessEngineCallbackController> logger,
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
        [FromBody] ProcessEngineAppCallbackPayload payload,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = _telemetry?.StartProcessEngineCallbackActivity(instanceGuid, commandKey);

        var appId = new AppIdentifier(org, app);
        var instanceId = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        IProcessEngineCommand? command = _serviceProvider
            .GetServices<IProcessEngineCommand>()
            .FirstOrDefault(x => x.GetKey() == commandKey);

        if (command is null)
        {
            _logger.LogError(
                "Handler not found for command key '{CommandKey}'. Instance: {InstanceId}.",
                commandKey,
                instanceId
            );
            activity?.SetStatus(ActivityStatusCode.Error, "Handler not found");
            return NotFound(
                new ProcessEngineCallbackErrorResponse
                {
                    Message = $"No handler registered for command key: {commandKey}",
                    ExceptionType = "HandlerNotFoundException",
                }
            );
        }

        Instance instance = await _instanceClient.GetInstance(
            appId.App,
            appId.Org,
            instanceOwnerPartyId,
            instanceId.InstanceGuid,
            StorageAuthenticationMethod.ServiceOwner()
        );

        string? currentTaskId = instance.Process?.CurrentTask?.ElementId;

        InstanceDataUnitOfWork instanceDataUnitOfWork = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            instance.Process?.CurrentTask?.ElementId,
            payload.Actor.Language
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
            _logger.LogError(
                "Data abandonment detected during callback. CommandKey: {CommandKey}, Instance: {InstanceId}, Task: {TaskId}",
                commandKey,
                instanceId,
                currentTaskId
            );
            activity?.SetStatus(ActivityStatusCode.Error, "Data abandonment detected");
            return BadRequest(
                new ProcessEngineCallbackErrorResponse
                {
                    Message = "Data abandonment detected. One or more data elements could not be saved.",
                    ExceptionType = "DataAbandonmentException",
                }
            );
        }

        switch (result)
        {
            case SuccessfulProcessEngineCommandResult:
                DataElementChanges changes = instanceDataUnitOfWork.GetDataElementChanges(false);
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
                    new ProcessEngineCallbackErrorResponse
                    {
                        Message = failed.ErrorMessage,
                        ExceptionType = failed.ExceptionType,
                    }
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
