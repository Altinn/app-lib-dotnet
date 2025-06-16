using System.Security.Claims;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process;

internal interface IProcessNextService
{
    /// <summary>
    /// Run process next
    /// </summary>
    Task<(ProcessChangeResult, Instance)> DoProcessNext(ProcessNextParams parameters, CancellationToken ct = default);
}

/// <summary>
/// Service for running all logic related to the process next.
/// </summary>
internal class ProcessNextService(
    IInstanceClient instanceClient,
    IProcessEngine processEngine,
    IProcessReader processReader,
    IProcessEngineAuthorizer processEngineAuthorizer,
    IValidationService validationService,
    IServiceProvider serviceProvider,
    ILogger<ProcessNextService> logger
) : IProcessNextService
{
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer =
        serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();

    /// <summary>
    /// Run process next
    /// </summary>
    public async Task<(ProcessChangeResult, Instance)> DoProcessNext(
        ProcessNextParams parameters,
        CancellationToken ct = default
    )
    {
        Instance instance = await instanceClient.GetInstance(
            parameters.App,
            parameters.Org,
            parameters.InstanceOwnerPartyId,
            parameters.InstanceGuid
        );

        string? currentTaskId = instance.Process.CurrentTask?.ElementId;

        if (currentTaskId is null)
        {
            return (
                new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Conflict,
                    ErrorMessage = "Process is not started. Use start!",
                },
                instance
            );
        }

        if (instance.Process.Ended.HasValue)
        {
            return (
                new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Conflict,
                    ErrorMessage = "Process is ended.",
                },
                instance
            );
        }

        string? altinnTaskType = instance.Process.CurrentTask?.AltinnTaskType;

        if (altinnTaskType == null)
        {
            return (
                new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Conflict,
                    ErrorMessage = "Instance does not have current altinn task type information!",
                },
                instance
            );
        }

        bool authorized = await processEngineAuthorizer.AuthorizeProcessNext(instance, parameters.Action);

        if (!authorized)
        {
            return (
                new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Unauthorized,
                    ErrorMessage =
                        $"User is not authorized to perform process next. Task ID: {currentTaskId}. Task type: {altinnTaskType}. Action: {parameters.Action ?? "none"}.",
                },
                instance
            );
        }

        logger.LogDebug(
            "User successfully authorized to perform process next. Task ID: {CurrentTaskId}. Task type: {AltinnTaskType}. Action: {ProcessNextAction}.",
            currentTaskId,
            altinnTaskType,
            LogSanitizer.Sanitize(parameters.Action ?? "none")
        );

        string checkedAction = parameters.Action ?? ConvertTaskTypeToAction(altinnTaskType);

        var request = new ProcessNextRequest()
        {
            Instance = instance,
            User = parameters.User,
            Action = checkedAction,
            ActionOnBehalfOf = parameters.ActionOnBehalfOf,
            Language = parameters.Language,
        };

        // If the action is 'reject', we should not run any service task and there is no need to check for a user action handler, since 'reject' doesn't have one.
        if (parameters.Action is not "reject")
        {
            IServiceTask? serviceTask = processEngine.CheckIfServiceTask(altinnTaskType);
            if (serviceTask is not null)
            {
                ServiceTaskResult serviceActionResult = await processEngine.HandleServiceTask(serviceTask, request, ct);

                if (serviceActionResult.Result is ServiceTaskResult.ResultType.Failure)
                {
                    return (
                        new ProcessChangeResult()
                        {
                            Success = false,
                            ErrorMessage = serviceActionResult.ErrorMessage,
                            ErrorType = serviceActionResult.ErrorType,
                        },
                        instance
                    );
                }
            }
            else
            {
                if (parameters.Action is not null)
                {
                    UserActionResult userActionResult = await processEngine.HandleUserAction(request, ct);

                    if (userActionResult.ResultType is ResultType.Failure)
                    {
                        return (
                            new ProcessChangeResult()
                            {
                                Success = false,
                                ErrorMessage = $"Action handler for action {request.Action} failed!",
                                ErrorType = userActionResult.ErrorType,
                            },
                            instance
                        );
                    }
                }
            }
        }

        // If the action is 'reject' the task is being abandoned, and we should skip validation, but only if reject has been allowed for the task in bpmn.
        if (checkedAction == "reject" && processReader.IsActionAllowedForTask(currentTaskId, checkedAction))
        {
            logger.LogInformation(
                "Skipping validation during process next because the action is 'reject' and the task is being abandoned."
            );
        }
        else
        {
            InstanceDataUnitOfWork dataAccessor = await _instanceDataUnitOfWorkInitializer.Init(
                instance,
                currentTaskId,
                parameters.Language
            );

            List<ValidationIssueWithSource> validationIssues = await validationService.ValidateInstanceAtTask(
                dataAccessor,
                currentTaskId, // run full validation
                ignoredValidators: null,
                onlyIncrementalValidators: null,
                language: parameters.Language
            );

            int errorCount = validationIssues.Count(v => v.Severity == ValidationIssueSeverity.Error);

            if (errorCount > 0)
            {
                return (
                    new ProcessChangeResult
                    {
                        Success = false,
                        ErrorType = ProcessErrorType.Conflict,
                        ErrorTitle = "Validation errors found",
                        ErrorMessage = $"{errorCount} validation errors found for task {currentTaskId}",
                        ValidationIssues = validationIssues,
                    },
                    instance
                );
            }
        }

        return (await processEngine.Next(request), instance);
    }

    private static string ConvertTaskTypeToAction(string actionOrTaskType)
    {
        switch (actionOrTaskType)
        {
            case "data":
            case "feedback":
            case "pdf":
            case "eFormidling":
                return "write";
            case "confirmation":
                return "confirm";
            case "signing":
                return "sign";
            default:
                // Not any known task type, so assume it is an action type
                return actionOrTaskType;
        }
    }
}

/// <summary>
///
/// </summary>
internal sealed record ProcessNextParams(
    string Org,
    string App,
    int InstanceOwnerPartyId,
    Guid InstanceGuid,
    ClaimsPrincipal User,
    string? Language,
    string? Action,
    string? ActionOnBehalfOf
);
