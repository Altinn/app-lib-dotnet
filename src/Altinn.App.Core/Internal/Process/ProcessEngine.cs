using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.App.Core.Internal.ProcessEngine;
using Altinn.App.Core.Internal.ProcessEngine.Http;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Altinn.App.Core.Models.Validation;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppProcessNextRequest = Altinn.App.Core.Models.Process.ProcessNextRequest;
using ProcessNextRequest = Altinn.App.ProcessEngine.Models.ProcessNextRequest;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Default implementation of the <see cref="IProcessEngine"/>
/// </summary>
internal class ProcessEngine : IProcessEngine
{
    private const int MaxNextIterationsAllowed = 100;

    private readonly IProcessReader _processReader;
    private readonly IProcessNavigator _processNavigator;
    private readonly UserActionService _userActionService;
    private readonly Telemetry? _telemetry;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IProcessEngineAuthorizer _processEngineAuthorizer;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly IValidationService _validationService;
    private readonly ProcessNextRequestFactory _processNextRequestFactory;
    private readonly IProcessEngineClient _processEngineClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngine"/> class.
    /// </summary>
    public ProcessEngine(
        IProcessReader processReader,
        IProcessNavigator processNavigator,
        UserActionService userActionService,
        IAuthenticationContext authenticationContext,
        IServiceProvider serviceProvider,
        IProcessEngineAuthorizer processEngineAuthorizer,
        IValidationService validationService,
        IProcessEngineClient processEngineClient,
        ILogger<ProcessEngine> logger,
        Telemetry? telemetry = null
    )
    {
        _processReader = processReader;
        _processNavigator = processNavigator;
        _userActionService = userActionService;
        _telemetry = telemetry;
        _authenticationContext = authenticationContext;
        _processEngineAuthorizer = processEngineAuthorizer;
        _validationService = validationService;
        _logger = logger;
        _processEngineClient = processEngineClient;
        _processNextRequestFactory = serviceProvider.GetRequiredService<ProcessNextRequestFactory>();
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
        _instanceDataUnitOfWorkInitializer = serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();
    }

    /// <inheritdoc/>
    public async Task<ProcessChangeResult> CreateInitialProcessState(ProcessStartRequest processStartRequest)
    {
        using var activity = _telemetry?.StartProcessStartActivity(processStartRequest.Instance);

        if (processStartRequest.Instance.Process != null)
        {
            var result = new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = "Process is already started. Use next.",
                ErrorType = ProcessErrorType.Conflict,
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        string validStartElement;
        try
        {
            validStartElement = ProcessHelper.GetValidStartEventOrError(
                processStartRequest.StartEventId,
                _processReader.GetStartEventIds()
            );
        }
        catch (ProcessException e)
        {
            var result = new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = e.Message,
                ErrorType = ProcessErrorType.Conflict,
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        // start process
        ProcessStateChange? startChange = await ProcessStart(processStartRequest.Instance, validStartElement);
        InstanceEvent? startEvent = startChange?.Events?[0].CopyValues();
        ProcessStateChange? nextChange = await MoveProcessStateToNextAndGenerateEvents(processStartRequest.Instance);
        InstanceEvent? goToNextEvent = nextChange?.Events?[0].CopyValues();
        List<InstanceEvent> events = [];
        if (startEvent is not null)
        {
            events.Add(startEvent);
        }

        if (goToNextEvent is not null)
        {
            events.Add(goToNextEvent);
        }

        ProcessStateChange processStateChange = new()
        {
            OldProcessState = startChange?.OldProcessState,
            NewProcessState = nextChange?.NewProcessState,
            Events = events,
        };

        _telemetry?.ProcessStarted();

        var changeResult = new ProcessChangeResult() { Success = true, ProcessStateChange = processStateChange };
        activity?.SetProcessChangeResult(changeResult);
        return changeResult;
    }

    /// <inheritdoc/>
    public async Task SubmitInitialProcessState(
        Instance instance,
        ProcessStateChange processStateChange,
        CancellationToken ct = default
    )
    {
        ProcessNextRequest processNextRequest = await _processNextRequestFactory.Create(processStateChange);
        await _processEngineClient.ProcessNext(instance, processNextRequest, ct);
        await WaitForEngineResponse(instance, ct);
    }

    /// <inheritdoc/>
    public async Task<ProcessChangeResult> Next(AppProcessNextRequest request, CancellationToken ct = default)
    {
        using Activity? activity = _telemetry?.StartProcessNextActivity(request.Instance, request.Action);

        Instance instance = request.Instance;

        if (
            !TryGetCurrentTaskIdAndAltinnTaskType(
                instance,
                out CurrentTaskIdAndAltinnTaskType? currentTaskIdAndAltinnTaskType,
                out ProcessChangeResult? invalidProcessStateError
            )
        )
        {
            activity?.SetProcessChangeResult(invalidProcessStateError);
            return invalidProcessStateError;
        }

        (string currentTaskId, string altinnTaskType) = currentTaskIdAndAltinnTaskType;

        bool authorized = await _processEngineAuthorizer.AuthorizeProcessNext(instance, request.Action);

        if (!authorized)
        {
            var result = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Unauthorized,
                ErrorMessage =
                    $"User is not authorized to perform process next. Task ID: {LogSanitizer.Sanitize(currentTaskId)}. Task type: {LogSanitizer.Sanitize(altinnTaskType)}. Action: {LogSanitizer.Sanitize(request.Action ?? "none")}.",
            };
            activity?.SetProcessChangeResult(result);
            return result;
        }

        _logger.LogDebug(
            "User successfully authorized to perform process next. Task ID: {CurrentTaskId}. Task type: {AltinnTaskType}. Action: {ProcessNextAction}.",
            LogSanitizer.Sanitize(currentTaskId),
            LogSanitizer.Sanitize(altinnTaskType),
            LogSanitizer.Sanitize(request.Action ?? "none")
        );

        string checkedAction = request.Action ?? ConvertTaskTypeToAction(altinnTaskType);
        bool isServiceTask = CheckIfServiceTask(altinnTaskType) is not null;
        string? processNextAction = request.Action;

        // If the action is 'reject', we should not run any service task and there is no need to check for a user action handler, since 'reject' doesn't have one.
        if (request.Action is not "reject")
        {
            if (request.Action is not null)
            {
                UserActionResult userActionResult = await HandleUserAction(instance, request, ct);

                if (userActionResult.ResultType is ResultType.Failure)
                {
                    var result = new ProcessChangeResult()
                    {
                        Success = false,
                        ErrorMessage = $"Action handler for action {LogSanitizer.Sanitize(request.Action)} failed!",
                        ErrorType = userActionResult.ErrorType,
                    };
                    activity?.SetProcessChangeResult(result);
                    return result;
                }
            }
        }

        // If the action is 'reject' the task is being abandoned, and we should skip validation, but only if reject has been allowed for the task in bpmn.
        if (checkedAction == "reject" && _processReader.IsActionAllowedForTask(currentTaskId, checkedAction))
        {
            _logger.LogInformation(
                "Skipping validation during process next because the action is 'reject' and the task is being abandoned."
            );
        }
        else if (isServiceTask)
        {
            _logger.LogInformation("Skipping validation during process next because the task is a service task.");
        }
        else
        {
            InstanceDataUnitOfWork dataAccessor = await _instanceDataUnitOfWorkInitializer.Init(
                instance,
                currentTaskId,
                request.Language
            );

            List<ValidationIssueWithSource> validationIssues = await _validationService.ValidateInstanceAtTask(
                dataAccessor,
                currentTaskId, // run full validation
                ignoredValidators: null,
                onlyIncrementalValidators: null,
                language: request.Language
            );

            int errorCount = validationIssues.Count(v => v.Severity == ValidationIssueSeverity.Error);

            if (errorCount > 0)
            {
                var result = new ProcessChangeResult
                {
                    Success = false,
                    ErrorType = ProcessErrorType.Conflict,
                    ErrorTitle = "Validation failed for task",
                    ErrorMessage = $"{errorCount} validation errors found for task {currentTaskId}",
                    ValidationIssues = validationIssues,
                };
                activity?.SetProcessChangeResult(result);
                return result;
            }
        }

        MoveToNextResult moveToNextResult = await HandleMoveToNext(instance, processNextAction, ct);

        var changeResult = new ProcessChangeResult(mutatedInstance: instance)
        {
            Success = true,
            ProcessStateChange = moveToNextResult.ProcessStateChange,
        };

        activity?.SetProcessChangeResult(changeResult);
        return changeResult;
    }

    private async Task<UserActionResult> HandleUserAction(
        Instance instance,
        AppProcessNextRequest request,
        CancellationToken ct
    )
    {
        Authenticated currentAuth = _authenticationContext.Current;
        IUserAction? actionHandler = _userActionService.GetActionHandler(request.Action);

        if (actionHandler is null)
            return UserActionResult.SuccessResult();

        InstanceDataUnitOfWork cachedDataMutator = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            taskId: null,
            request.Language
        );

        int? userId = currentAuth switch
        {
            Authenticated.User auth => auth.UserId,
            _ => null,
        };

        UserActionResult actionResult = await actionHandler.HandleAction(
            new UserActionContext(
                cachedDataMutator,
                userId,
                language: request.Language,
                authentication: currentAuth,
                onBehalfOf: request.ActionOnBehalfOf,
                cancellationToken: ct
            )
        );

        if (actionResult.ResultType == ResultType.Failure)
        {
            return actionResult;
        }

        if (cachedDataMutator.HasAbandonIssues)
        {
            throw new Exception(
                "Abandon issues found in data elements. Abandon issues should be handled by the action handler."
            );
        }

        DataElementChanges changes = cachedDataMutator.GetDataElementChanges(initializeAltinnRowId: false);
        await cachedDataMutator.UpdateInstanceData(changes);
        await cachedDataMutator.SaveChanges(changes);

        return actionResult;
    }

    /// <summary>
    /// Does not save process. Instance object is updated.
    /// </summary>
    private async Task<ProcessStateChange?> ProcessStart(Instance instance, string startEvent)
    {
        if (instance.Process != null)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        ProcessState startState = new()
        {
            Started = now,
            StartEvent = startEvent,
            CurrentTask = new ProcessElementInfo { Flow = 1, ElementId = startEvent },
        };

        instance.Process = startState;

        List<InstanceEvent> events =
        [
            await GenerateProcessChangeEvent(InstanceEventType.process_StartEvent.ToString(), instance, now),
        ];

        // ! TODO: should probably improve nullability handling in the next major version
        return new ProcessStateChange
        {
            OldProcessState = null!,
            NewProcessState = startState,
            Events = events,
        };
    }

    /// <summary>
    /// Moves instance's process to nextElement id. Returns the instance together with process events.
    /// </summary>
    private async Task<ProcessStateChange?> MoveProcessStateToNextAndGenerateEvents(
        Instance instance,
        string? action = null
    )
    {
        if (instance.Process == null)
        {
            return null;
        }

        ProcessStateChange result = new()
        {
            OldProcessState = new ProcessState()
            {
                Started = instance.Process.Started,
                CurrentTask = instance.Process.CurrentTask,
                StartEvent = instance.Process.StartEvent,
            },
            Events = await GenerateEventsAndUpdateProcessState(instance, action),
            NewProcessState = instance.Process,
        };
        return result;
    }

    private async Task<List<InstanceEvent>> GenerateEventsAndUpdateProcessState(
        Instance instance,
        string? action = null
    )
    {
        List<InstanceEvent> events = [];

        ProcessState previousState = instance.Process.Copy();
        ProcessState currentState = instance.Process;
        string? previousElementId = currentState.CurrentTask?.ElementId;

        ProcessElement? nextElement = await _processNavigator.GetNextTask(
            instance,
            instance.Process.CurrentTask.ElementId,
            action
        );
        DateTime now = DateTime.UtcNow;
        // ending previous element if task
        if (_processReader.IsProcessTask(previousElementId))
        {
            instance.Process = previousState;
            var eventType = InstanceEventType.process_EndTask.ToString();
            if (action is "reject")
            {
                eventType = InstanceEventType.process_AbandonTask.ToString();
            }

            events.Add(await GenerateProcessChangeEvent(eventType, instance, now));
            instance.Process = currentState;
        }

        // ending process if next element is end event
        if (nextElement is null)
        {
            throw new ProcessException("Next process element was unexpectedly null");
        }
        var nextElementId = nextElement.Id;
        if (_processReader.IsEndEvent(nextElementId))
        {
            using var activity = _telemetry?.StartProcessEndActivity(instance);

            currentState.CurrentTask = null;
            currentState.Ended = now;
            currentState.EndEvent = nextElementId;

            events.Add(await GenerateProcessChangeEvent(InstanceEventType.process_EndEvent.ToString(), instance, now));

            // add submit event (to support Altinn2 SBL)
            events.Add(await GenerateProcessChangeEvent(InstanceEventType.Submited.ToString(), instance, now));
        }
        else if (_processReader.IsProcessTask(nextElementId))
        {
            var task = nextElement as ProcessTask;
            currentState.CurrentTask = new ProcessElementInfo
            {
                Flow = currentState.CurrentTask?.Flow + 1,
                ElementId = nextElementId,
                Name = nextElement.Name,
                Started = now,
                AltinnTaskType = task?.ExtensionElements?.TaskExtension?.TaskType,
                FlowType = action is "reject"
                    ? ProcessSequenceFlowType.AbandonCurrentMoveToNext.ToString()
                    : ProcessSequenceFlowType.CompleteCurrentMoveToNext.ToString(),
            };

            events.Add(await GenerateProcessChangeEvent(InstanceEventType.process_StartTask.ToString(), instance, now));
        }

        // current state points to the instance's process object. The following statement is unnecessary, but clarifies logic.
        instance.Process = currentState;

        return events;
    }

    private async Task<InstanceEvent> GenerateProcessChangeEvent(string eventType, Instance instance, DateTime now)
    {
        var currentAuth = _authenticationContext.Current;
        PlatformUser user;
        switch (currentAuth)
        {
            case Authenticated.User auth:
            {
                var details = await auth.LoadDetails(validateSelectedParty: true);
                user = new PlatformUser
                {
                    UserId = auth.UserId,
                    AuthenticationLevel = auth.AuthenticationLevel,
                    NationalIdentityNumber = details.Profile.Party.SSN,
                };
                break;
            }
            case Authenticated.Org:
            {
                user = new PlatformUser { }; // TODO: what do we do here?
                break;
            }
            case Authenticated.ServiceOwner auth:
            {
                user = new PlatformUser { OrgId = auth.Name, AuthenticationLevel = auth.AuthenticationLevel };
                break;
            }
            case Authenticated.SystemUser auth:
            {
                user = new PlatformUser
                {
                    SystemUserId = auth.SystemUserId[0],
                    SystemUserOwnerOrgNo = auth.SystemUserOrgNr.Get(Models.OrganisationNumberFormat.Local),
                    SystemUserName = null, // TODO: will get this name later when a lookup API is implemented or the name is passed in token
                    AuthenticationLevel = auth.AuthenticationLevel,
                };
                break;
            }
            default:
                throw new Exception($"Unknown authentication context: {currentAuth.GetType().Name}");
        }

        InstanceEvent instanceEvent = new()
        {
            InstanceId = instance.Id,
            InstanceOwnerPartyId = instance.InstanceOwner.PartyId,
            EventType = eventType,
            Created = now,
            User = user,
            ProcessInfo = instance.Process,
        };

        return instanceEvent;
    }

    private async Task<MoveToNextResult> HandleMoveToNext(
        Instance instance,
        string? action,
        CancellationToken ct = default
    )
    {
        ProcessStateChange? processStateChange = await MoveProcessStateToNextAndGenerateEvents(instance, action);

        if (processStateChange is null)
        {
            return new MoveToNextResult(instance, null);
        }

        ProcessNextRequest processNextRequest = await _processNextRequestFactory.Create(processStateChange);
        await _processEngineClient.ProcessNext(instance, processNextRequest, ct);

        await WaitForEngineResponse(instance, ct);

        return new MoveToNextResult(instance, processStateChange);
    }

    /// <summary>
    /// Runs IProcessEnd implementations defined in the app.
    /// </summary>
    private async Task RunAppDefinedProcessEndHandlers(Instance instance, List<InstanceEvent>? events)
    {
        var processEnds = _appImplementationFactory.GetAll<IProcessEnd>().ToList();
        if (processEnds.Count is 0)
            return;

        using var mainActivity = _telemetry?.StartProcessEndHandlersActivity(instance);

        foreach (IProcessEnd processEnd in processEnds)
        {
            using var nestedActivity = _telemetry?.StartProcessEndHandlerActivity(instance, processEnd);
            await processEnd.End(instance, events);
        }
    }

    private sealed record MoveToNextResult(Instance Instance, ProcessStateChange? ProcessStateChange)
    {
        [MemberNotNullWhen(true, nameof(ProcessStateChange))]
        public bool IsEndEvent => ProcessStateChange?.NewProcessState?.Ended is not null;
    };

    internal static string ConvertTaskTypeToAction(string actionOrTaskType)
    {
        switch (actionOrTaskType)
        {
            case "data":
            case "feedback":
            case "pdf":
            case "eFormidling":
            case "fiksArkiv":
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

    private static bool TryGetCurrentTaskIdAndAltinnTaskType(
        Instance instance,
        [NotNullWhen(true)] out CurrentTaskIdAndAltinnTaskType? state,
        [NotNullWhen(false)] out ProcessChangeResult? error
    )
    {
        state = null; // allowed because the method may return false
        error = null;

        ProcessState? process = instance.Process;

        if (process is null)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "The instance is missing process information.",
            };
            return false;
        }

        if (process.Ended is not null)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Process is ended.",
            };
            return false;
        }

        if (process.CurrentTask?.ElementId is not string taskId)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Process is not started. Use start!",
            };
            return false;
        }

        if (process.CurrentTask.AltinnTaskType is not string taskType)
        {
            error = new ProcessChangeResult
            {
                Success = false,
                ErrorType = ProcessErrorType.Conflict,
                ErrorMessage = "Instance does not have current altinn task type information!",
            };
            return false;
        }

        state = new CurrentTaskIdAndAltinnTaskType(taskId, taskType);
        return true;
    }

    private async Task WaitForEngineResponse(Instance instance, CancellationToken ct)
    {
        var overallStopwatch = Stopwatch.StartNew();
        const int timeoutMs = 100_000;
        const int initialDelayMs = 100;
        const int maxDelayMs = 2_000;
        int currentDelayMs = initialDelayMs;

        while (!ct.IsCancellationRequested)
        {
            ProcessEngineStatusResponse? processStatus = await _processEngineClient.GetActiveJobStatus(instance, ct);

            if (processStatus is null)
            {
                return;
            }

            switch (processStatus.OverallStatus)
            {
                case ProcessEngineItemStatus.Canceled:
                    throw new InvalidOperationException("Process engine job was canceled.");
                case ProcessEngineItemStatus.Failed:
                    throw new InvalidOperationException("Process engine job failed.");
                case ProcessEngineItemStatus.Completed:
                    return;
                case ProcessEngineItemStatus.Enqueued:
                case ProcessEngineItemStatus.Processing:
                case ProcessEngineItemStatus.Requeued:
                    _logger.LogDebug(
                        "Process engine job is still in progress. Status: {Status}",
                        processStatus.OverallStatus
                    );
                    break;

                default:
                    throw new InvalidOperationException(
                        "Received unknown process engine job status: " + processStatus.OverallStatus
                    );
            }

            if (overallStopwatch.ElapsedMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Timeout while waiting for process engine job to complete.");
            }

            await Task.Delay(currentDelayMs, ct);

            // Exponential backoff: double delay each iteration up to max
            currentDelayMs = Math.Min(currentDelayMs * 2, maxDelayMs);
        }

        throw new InvalidOperationException("Tried waiting for process engine job, but the operation was cancelled.");
    }

    private IServiceTask? CheckIfServiceTask(string? altinnTaskType)
    {
        if (altinnTaskType is null)
            return null;

        IEnumerable<IServiceTask> serviceTasks = _appImplementationFactory.GetAll<IServiceTask>();
        IServiceTask? serviceTask = serviceTasks.FirstOrDefault(x =>
            x.Type.Equals(altinnTaskType, StringComparison.OrdinalIgnoreCase)
        );

        return serviceTask;
    }

    private sealed record CurrentTaskIdAndAltinnTaskType(string CurrentTaskId, string AltinnTaskType);
}
